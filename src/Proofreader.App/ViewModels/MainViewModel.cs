using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;

namespace Proofreader.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISpellchecker _spellchecker;
    private readonly IFileService _fileService; // <-- ИЗМЕНЕНО
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private string _inputText = "";

    public ObservableCollection<SpellingError> Errors { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Готов к работе";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private SpellingError? _selectedError;

    public event Action<SpellingError>? ErrorSelected;

    // Внедряем сервисы
    public MainViewModel(
        ISpellchecker spellchecker,
        IFileService fileService, // <-- ИЗМЕНЕНО
        IFileDialogService fileDialogService)
    {
        _spellchecker = spellchecker;
        _fileService = fileService; // <-- ИЗМЕНЕНО
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
        var text = await _fileService.LoadTextAsync(path); // <-- ИЗМЕНЕНО
        
        if (text != null)
        {
            InputText = text;
            Errors.Clear(); // Очищаем старые ошибки при загрузке нового файла
            StatusMessage = $"Файл загружен: {System.IO.Path.GetFileName(path)}";
        }
        else
        {
            StatusMessage = "Ошибка при чтении файла.";
        }
    }

    // КОМАНДА: Сохранить файл
    [RelayCommand]
    private async Task SaveFileAsync()
    {
        var path = await _fileDialogService.PickTextFileToSaveAsync();
        if (string.IsNullOrEmpty(path)) return;

        StatusMessage = "Сохранение файла...";
        try
        {
            await _fileService.SaveTextAsync(path, InputText); // <-- ИЗМЕНЕНО
            StatusMessage = $"Файл сохранен: {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception)
        {
            StatusMessage = "Ошибка при сохранении файла.";
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