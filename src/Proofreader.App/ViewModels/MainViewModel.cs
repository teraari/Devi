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

    [ObservableProperty]
    private string _inputText = "Вставьте или напишите ваш текст здесь для проверки...";

    public ObservableCollection<SpellingError> Errors { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Готов к проверке";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private SpellingError? _selectedError;

    public event Action<SpellingError>? ErrorSelected;

    public MainViewModel(ISpellchecker spellchecker)
    {
        _spellchecker = spellchecker;
    }

    partial void OnSelectedErrorChanged(SpellingError? value)
    {
        if (value != null)
        {
            ErrorSelected?.Invoke(value);
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
                // ДОБАВЛЯЕМ: Выдаем каждой ошибке команду для замены
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

    // НОВЫЙ МЕТОД: Применение исправления
    private void ApplyFix(SpellingError error, string? suggestion)
    {
        if (string.IsNullOrEmpty(suggestion)) return;

        // 1. Заменяем слово в тексте
        // Берем часть текста до ошибки + подсказку + часть текста после ошибки
        string textBefore = InputText.Substring(0, error.StartIndex);
        string textAfter = InputText.Substring(error.StartIndex + error.Length);
        
        InputText = textBefore + suggestion + textAfter;

        // 2. Удаляем эту ошибку из списка
        Errors.Remove(error);

        // 3. Сдвигаем индексы всех последующих ошибок, так как длина текста изменилась
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