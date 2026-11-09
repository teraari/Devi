using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Proofreader.App.ViewModels;

public class ShortcutItem
{
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

public partial class ShortcutsViewModel : ViewModelBase
{
    private readonly List<ShortcutItem> _allShortcuts = new();
    public ObservableCollection<ShortcutItem> FilteredShortcuts { get; } = new();

    [ObservableProperty] private string _searchText = string.Empty;

    public ShortcutsViewModel()
    {
        LoadShortcuts();
        UpdateFilter();
    }

    partial void OnSearchTextChanged(string value) => UpdateFilter();

    private void UpdateFilter()
    {
        FilteredShortcuts.Clear();
        var filtered = _allShortcuts.Where(s => 
            string.IsNullOrWhiteSpace(SearchText) || 
            s.Action.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
            s.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            s.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered) FilteredShortcuts.Add(item);
    }

    private void LoadShortcuts()
    {
        _allShortcuts.Add(new ShortcutItem { Category = "Файл", Action = "Новый документ", Key = "Ctrl + N" });
        _allShortcuts.Add(new ShortcutItem { Category = "Файл", Action = "Открыть документ", Key = "Ctrl + O" });
        _allShortcuts.Add(new ShortcutItem { Category = "Файл", Action = "Сохранить", Key = "Ctrl + S" });
        _allShortcuts.Add(new ShortcutItem { Category = "Файл", Action = "Закрыть документ", Key = "Ctrl + W" });
        
        _allShortcuts.Add(new ShortcutItem { Category = "Вид", Action = "Увеличить масштаб", Key = "Ctrl + Plus" });
        _allShortcuts.Add(new ShortcutItem { Category = "Вид", Action = "Уменьшить масштаб", Key = "Ctrl + Minus" });
        _allShortcuts.Add(new ShortcutItem { Category = "Вид", Action = "Сбросить масштаб", Key = "Ctrl + 0" });

        _allShortcuts.Add(new ShortcutItem { Category = "Проверка", Action = "Проверить документ", Key = "F5" });
        _allShortcuts.Add(new ShortcutItem { Category = "Проверка", Action = "Следующая ошибка", Key = "F8" });
        _allShortcuts.Add(new ShortcutItem { Category = "Проверка", Action = "Предыдущая ошибка", Key = "Shift + F8" });
    }
}