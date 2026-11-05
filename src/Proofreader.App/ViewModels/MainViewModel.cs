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

    // ДОБАВЛЯЕМ: Выбранная ошибка из списка
    [ObservableProperty]
    private SpellingError? _selectedError;

    // Событие, на которое подпишется UI, чтобы выделить текст
    public event Action<SpellingError>? ErrorSelected;

    public MainViewModel(ISpellchecker spellchecker)
    {
        _spellchecker = spellchecker;
    }

    // Этот метод срабатывает автоматически при изменении SelectedError
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
}