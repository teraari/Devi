using System;
using System.Collections.Generic;
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

    [ObservableProperty]
    private string _currentFileName = "Новый_документ.docx";

    // НОВОЕ СВОЙСТВО: Хранит путь к исходному файлу (чтобы при сохранении взять его за основу)
    [ObservableProperty]
    private string? _originalFilePath;

    public ObservableCollection<SpellingError> Errors { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Готов к работе";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private SpellingError? _selectedError;

    public event Action<SpellingError>? ErrorSelected;

    // НОВОЕ: Скрытый список для хранения истории исправлений (для сохранения в DOCX)
    private readonly List<SpellingError> _appliedFixes = new();

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
            CurrentFileName = Path.GetFileName(path);
            OriginalFilePath = path; // <-- ЗАПОМИНАЕМ ИСХОДНЫЙ ПУТЬ
            
            Errors.Clear(); 
            _appliedFixes.Clear(); // Очищаем историю исправлений при открытии нового файла
            
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
        var path = await _fileDialogService.PickTextFileToSaveAsync(CurrentFileName);
        if (string.IsNullOrEmpty(path)) return;

        StatusMessage = "Сохранение файла...";
        try
        {
            // Передаем оригинальный путь и список примененных исправлений
            await _fileService.SaveTextAsync(path, InputText, OriginalFilePath, _appliedFixes);
            
            CurrentFileName = Path.GetFileName(path);
            OriginalFilePath = path; // Сохраненный файл становится новым оригиналом
            
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
        _appliedFixes.Clear(); // Очищаем историю исправлений перед новой проверкой

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

        // 1. Заменяем слово в тексте
        string textBefore = InputText.Substring(0, error.StartIndex);
        string textAfter = InputText.Substring(error.StartIndex + error.Length);
        InputText = textBefore + suggestion + textAfter;

        // 2. Запоминаем исправление для OpenXML
        error.AppliedSuggestion = suggestion;
        _appliedFixes.Add(error);

        // 3. Убираем ошибку из визуального списка
        Errors.Remove(error);

        // 4. Сдвигаем индексы оставшихся ошибок, так как длина текста изменилась
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