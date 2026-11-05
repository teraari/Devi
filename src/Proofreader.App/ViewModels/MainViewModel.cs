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

    // Текст, который вводит пользователь
    [ObservableProperty]
    private string _inputText = "Вставьте или напишите ваш текст здесь для проверки...";

    // Список найденных ошибок
    public ObservableCollection<SpellingError> Errors { get; } = new();

    // Статус проверки
    [ObservableProperty]
    private string _statusMessage = "Готов к проверке";

    [ObservableProperty]
    private bool _isBusy;

    // Внедряем сервис проверки через конструктор
    public MainViewModel(ISpellchecker spellchecker)
    {
        _spellchecker = spellchecker;
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
        catch (System.Exception ex)
        {
            StatusMessage = $"Ошибка при проверке: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}