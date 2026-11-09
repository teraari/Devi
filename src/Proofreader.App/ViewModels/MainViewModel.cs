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
    private readonly IProofreaderEngine _engine;
    private readonly IFileService _fileService;
    private readonly IDictionaryService _dictionaryService;
    private readonly IFileDialogService? _fileDialogService;

    [ObservableProperty]
    private DocumentViewModel? _activeDocument;

    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible = true;

    [ObservableProperty]
    private bool _wordWrap = true;

    [ObservableProperty]
    private double _zoom = 1.0;

    [ObservableProperty]
    private string _statusMessage = "Готово";

    public ObservableCollection<DocumentViewModel> Documents { get; } = new();

    // События для окон
    public event Action<SpellingError>? ErrorSelected;
    public event Action? RequestShowAbout;
    public event Action? RequestShowDocumentation;
    public event Action? RequestShowShortcuts;

    public MainViewModel(
        IProofreaderEngine engine,
        IFileService fileService,
        IDictionaryService dictionaryService,
        IFileDialogService? fileDialogService = null)
    {
        _engine = engine;
        _fileService = fileService;
        _dictionaryService = dictionaryService;
        _fileDialogService = fileDialogService;

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActiveDocument) && ActiveDocument != null)
            {
                ActiveDocument.ErrorSelected += OnActiveDocumentErrorSelected;
            }
        };

        CreateDocument();
    }

    private void OnActiveDocumentErrorSelected(SpellingError error)
    {
        ErrorSelected?.Invoke(error);
    }

    // === Команды меню "Файл" ===
    [RelayCommand]
    private void CreateDocument()
    {
        var doc = new DocumentViewModel(_engine, _fileService, _dictionaryService, _fileDialogService);
        Documents.Add(doc);
        ActiveDocument = doc;
        StatusMessage = "Создан новый документ";
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (_fileDialogService == null) return;
        var path = await _fileDialogService.PickTextFileToOpenAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        var doc = new DocumentViewModel(_engine, _fileService, _dictionaryService, _fileDialogService);
        await doc.LoadDocumentAsync(path);
        
        Documents.Add(doc);
        ActiveDocument = doc;
        StatusMessage = $"Открыт файл: {doc.FileName}";
    }

    [RelayCommand]
    private void CloseDocument()
    {
        if (ActiveDocument != null)
        {
            Documents.Remove(ActiveDocument);
            ActiveDocument = Documents.Count > 0 ? Documents[0] : null;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    // === Команды меню "Правка" ===
    [RelayCommand] private void Undo() { /* Заглушка для UI */ }
    [RelayCommand] private void Redo() { /* Заглушка для UI */ }
    [RelayCommand] private void Cut() { /* Заглушка для UI */ }
    [RelayCommand] private void Copy() { /* Заглушка для UI */ }
    [RelayCommand] private void Paste() { /* Заглушка для UI */ }
    [RelayCommand] private void DeleteText() { /* Заглушка для UI */ }
    [RelayCommand] private void SelectAll() { /* Заглушка для UI */ }

    // === Команды меню "Вид" ===
    [RelayCommand] private void ToggleLeftPanel() => IsLeftPanelVisible = !IsLeftPanelVisible;
    [RelayCommand] private void ToggleRightPanel() => IsRightPanelVisible = !IsRightPanelVisible;
    [RelayCommand] private void ToggleWordWrap() => WordWrap = !WordWrap;
    
    [RelayCommand] private void ZoomIn() => Zoom += 0.1;
    [RelayCommand] private void ZoomOut() => Zoom = Math.Max(0.5, Zoom - 0.1);
    [RelayCommand] private void ResetZoom() => Zoom = 1.0;

    // === Команды меню "Справка" ===
    [RelayCommand] private void ShowAbout() => RequestShowAbout?.Invoke();
    [RelayCommand] private void ShowDocumentation() => RequestShowDocumentation?.Invoke();
    [RelayCommand] private void ShowShortcuts() => RequestShowShortcuts?.Invoke();
}