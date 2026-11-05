using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;

namespace Proofreader.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISpellchecker _spellchecker;
    private readonly IFileService _fileService;
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private string _inputText = "";

    // НОВОЕ СВОЙСТВО: Имя текущего файла (для диалога сохранения)
    [ObservableProperty]
    private string _currentFileName = "Новый_документ.docx";

    public ObservableCollection<SpellingError> Errors { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Готов к работе";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private SpellingError? _selectedError;

    public event Action<SpellingError>? ErrorSelected;

    public MainViewModel(
        ISpellchecker spellchecker,
        IFileService fileService,
        IFileDialogService fileDialogService)
    {
        _spellchecker = spellchecker;
        _fileService = fileService;
        _fileDialogService = fileDialogService;
    }

    partial void OnSelectedErrorChanged(SpellingError? value)
    {
        if (value != null)
        {
            ErrorSelected?.Invoke(value);
        }
    }

    // КОМАНДА: Открыть файл
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var path = await _fileDialogService.PickTextFileToOpenAsync();
        if (string.IsNullOrEmpty(path)) return;

        StatusMessage = "Загрузка файла...";
        var text = await _fileService.LoadTextAsync(path);
        
        if (text != null)
        {
            InputText = text;
            CurrentFileName = Path.GetFileName(path); // Обновляем имя файла
            Errors.Clear(); 
            StatusMessage = $"Файл загружен: {CurrentFileName}";
        }
        else
        {
            StatusMessage = "Ошибка при чтении файла (возможно, он поврежден или занят).";
        }
    }

    // КОМАНДА: Сохранить файл
    [RelayCommand]
    private async Task SaveFileAsync()
    {
        // Передаем текущее имя файла в диалоговое окно
        var path = await _fileDialogService.PickTextFileToSaveAsync(CurrentFileName);
        if (string.IsNullOrEmpty(path)) return;

        StatusMessage = "Сохранение файла...";
        try
        {
            await _fileService.SaveTextAsync(path, InputText);
            CurrentFileName = Path.GetFileName(path); // Обновляем имя после сохранения
            StatusMessage = $"Файл сохранен: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка при сохранении: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CheckTextAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        IsBusy = true;
        StatusMessage = "Идет проверка текста...";
        Errors.Clear();

        try
        {
            var foundErrors = await _spellchecker.CheckTextAsync(InputText);
            
            foreach (var error in foundErrors)
            {
                error.ApplySuggestionCommand = new RelayCommand<string>(suggestion => ApplyFix(error, suggestion));
                Errors.Add(error);
            }

            StatusMessage = $"Проверка завершена. Найдено ошибок: {Errors.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка при проверке: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFix(SpellingError error, string? suggestion)
    {
        if (string.IsNullOrEmpty(suggestion)) return;

        string textBefore = InputText.Substring(0, error.StartIndex);
        string textAfter = InputText.Substring(error.StartIndex + error.Length);
        
        InputText = textBefore + suggestion + textAfter;

        Errors.Remove(error);

        // Сдвигаем индексы оставшихся ошибок, так как длина текста изменилась
        int lengthDifference = suggestion.Length - error.Length;
        foreach (var remainingError in Errors)
        {
            if (remainingError.StartIndex > error.StartIndex)
            {
                remainingError.StartIndex += lengthDifference;
            }
        }

        StatusMessage = $"Исправлено: '{error.Word}' -> '{suggestion}'";
    }
}