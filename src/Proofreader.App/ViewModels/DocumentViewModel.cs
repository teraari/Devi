using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net; // Нужно для HTML-кодирования
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;
using Proofreader.Language;
using QuestPDF.Fluent; // Для PDF
using QuestPDF.Helpers; // Для PDF
using QuestPDF.Infrastructure; // Для PDF

namespace Proofreader.App.ViewModels;

public partial class DocumentViewModel : ViewModelBase
{
    private readonly IProofreaderEngine _engine;
    private readonly IFileService _fileService;
    private readonly IDictionaryService _dictionaryService;
    private readonly IFileDialogService? _fileDialogService;

    private readonly List<SpellingError> _allErrors = new();
    private readonly List<SpellingError> _fixedErrors = new();
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private string _fileName = "Новый документ";

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private string _checkStatus = "Ожидание проверки";

    [ObservableProperty]
    private string _checkTime = "—";

    [ObservableProperty]
    private SpellingError? _selectedError;

    [ObservableProperty]
    private string _currentFilter = "All";

    public ObservableCollection<SpellingError> FilteredErrors { get; } = new();
    public ObservableCollection<SpellingError> EditorErrors { get; } = new();

    public int ErrorCount => FilteredErrors.Count;

    public event Action<SpellingError>? ErrorSelected;

    public DocumentViewModel(IProofreaderEngine engine, IFileService fileService, IDictionaryService dictionaryService, IFileDialogService? fileDialogService = null)
    {
        _engine = engine;
        _fileService = fileService;
        _dictionaryService = dictionaryService;
        _fileDialogService = fileDialogService;

        FilteredErrors.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ErrorCount));
    }

    partial void OnInputTextChanged(string value)
    {
        IsSuccess = false;
        if (string.IsNullOrWhiteSpace(value)) WordCount = 0;
        else WordCount = value.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    partial void OnSelectedErrorChanged(SpellingError? value)
    {
        if (value != null) ErrorSelected?.Invoke(value);
    }

    public async Task LoadDocumentAsync(string path)
    {
        IsSuccess = false;
        CheckStatus = "Загрузка документа...";
        var text = await _fileService.LoadTextAsync(path);
        if (text == null) { CheckStatus = "Не удалось открыть документ"; return; }

        InputText = text;
        FilePath = path;
        FileName = Path.GetFileName(path);
        CheckStatus = "Документ загружен";
        _allErrors.Clear();
        _fixedErrors.Clear();
        FilterErrors();
    }

    [RelayCommand]
    private async Task CheckTextAsync()
    {
        IsSuccess = false;
        if (string.IsNullOrWhiteSpace(InputText))
        {
            _allErrors.Clear();
            FilterErrors();
            CheckStatus = "Документ пуст";
            CheckTime = "0";
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var stopwatch = Stopwatch.StartNew();
        CheckStatus = "Выполняется быстрая проверка...";
        _allErrors.Clear();
        _fixedErrors.Clear();

        try
        {
            var errors = await _engine.CheckDocumentAsync(InputText, token);
            if (token.IsCancellationRequested) return;

            foreach (var error in errors)
            {
                ConfigureErrorCommands(error);
                _allErrors.Add(error);
            }

            FilterErrors();
            stopwatch.Stop();
            CheckTime = $"{stopwatch.Elapsed.TotalSeconds:F2} с";
            
            IsSuccess = _allErrors.Count == 0;
            CheckStatus = IsSuccess ? "Ошибок не найдено." : $"Проверка завершена. Ошибок: {_allErrors.Count}";
        }
        catch (Exception ex) { CheckStatus = $"Ошибка: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task AiFixAsync()
    {
        IsSuccess = false;
        if (string.IsNullOrWhiteSpace(InputText)) return;
        
        if (WordCount > 1500)
        {
            CheckStatus = "Ошибка: Текст слишком большой для ИИ (максимум 1500 слов).";
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        CheckStatus = "Подготовка нейросети (если первый раз, ждите 20 сек)...";

        try
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionaries", "saiga.gguf");
            if (!File.Exists(modelPath)) { CheckStatus = "Ошибка: Файл saiga.gguf не найден!"; return; }

            var originalText = InputText;
            var editor = await TextEditingEngine.CreateAsync(modelPath);

            CheckStatus = "Нейросеть исправляет текст (идет генерация)...";
            var fixedText = await editor.FixErrorsAsync(originalText);

            InputText = fixedText;
            HighlightDifferences(originalText, fixedText);

            _allErrors.Clear(); 
            FilterErrors();

            stopwatch.Stop();
            CheckTime = $"{stopwatch.Elapsed.TotalSeconds:F2} с";
            IsSuccess = true;
            CheckStatus = "Нейросеть успешно исправила текст! Ошибок не найдено.";
        }
        catch (Exception ex)
        {
            CheckStatus = $"Ошибка ИИ: {ex.Message}";
        }
    }

    private void HighlightDifferences(string oldText, string newText)
    {
        _fixedErrors.Clear();
        var regex = new Regex(@"[\w']+|[^\w\s]");
        var oldMatches = regex.Matches(oldText).Cast<Match>().ToList();
        var newMatches = regex.Matches(newText).Cast<Match>().ToList();
        
        int m = oldMatches.Count;
        int n = newMatches.Count;
        int[,] dp = new int[m + 1, n + 1];
        
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (string.Equals(oldMatches[i - 1].Value, newMatches[j - 1].Value, StringComparison.OrdinalIgnoreCase))
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }
        
        bool[] isMatched = new bool[n];
        int x = m, y = n;
        while (x > 0 && y > 0)
        {
            if (string.Equals(oldMatches[x - 1].Value, newMatches[y - 1].Value, StringComparison.OrdinalIgnoreCase))
            {
                isMatched[y - 1] = true;
                x--; y--;
            }
            else if (dp[x - 1, y] >= dp[x, y - 1])
            {
                x--;
            }
            else
            {
                y--;
            }
        }
        
        for (int i = 0; i < n; i++)
        {
            if (!isMatched[i])
            {
                var match = newMatches[i];
                _fixedErrors.Add(new SpellingError
                {
                    StartIndex = match.Index,
                    Length = match.Length,
                    Word = match.Value,
                    IsAutoFixed = true,
                    ErrorType = ErrorType.Style
                });
            }
        }
    }

    [RelayCommand] private void CancelCheck() => _cts?.Cancel();
    
    private void ConfigureErrorCommands(SpellingError error)
    {
        error.ApplySuggestionCommand = new RelayCommand<string?>(s => ApplySuggestion(error, s));
        error.AddToDictionaryCommand = new AsyncRelayCommand(() => AddToDictionaryAsync(error));
    }

    private void ApplySuggestion(SpellingError error, string? suggestion)
    {
        if (string.IsNullOrEmpty(suggestion) || error.StartIndex < 0 || error.StartIndex + error.Length > InputText.Length) return;
        InputText = InputText.Remove(error.StartIndex, error.Length).Insert(error.StartIndex, suggestion);
        error.AppliedSuggestion = suggestion;
        _fixedErrors.Add(error);
        _allErrors.Remove(error);
        FilterErrors();
        IsSuccess = _allErrors.Count == 0;
    }

    private async Task AddToDictionaryAsync(SpellingError error)
    {
        if (string.IsNullOrWhiteSpace(error.Word)) return;
        await _dictionaryService.AddWordAsync(error.Word);
        _allErrors.Remove(error);
        FilterErrors();
        IsSuccess = _allErrors.Count == 0;
    }

    [RelayCommand] private void IgnoreError(SpellingError? error) 
    { 
        if (error == null) return; 
        _allErrors.Remove(error); 
        FilterErrors();
        IsSuccess = _allErrors.Count == 0;
    }
    
    [RelayCommand] private void SetFilter(string? filter) { CurrentFilter = string.IsNullOrWhiteSpace(filter) ? "All" : filter; FilterErrors(); }

    private void FilterErrors()
    {
        FilteredErrors.Clear();
        EditorErrors.Clear();

        IEnumerable<SpellingError> uiErrors = _allErrors;
        if (CurrentFilter != "All" && Enum.TryParse<ErrorType>(CurrentFilter, true, out var errorType))
            uiErrors = uiErrors.Where(e => e.ErrorType == errorType);

        foreach (var error in uiErrors.OrderBy(e => e.StartIndex)) 
            FilteredErrors.Add(error);

        foreach (var error in _allErrors.Concat(_fixedErrors).OrderBy(e => e.StartIndex))
            EditorErrors.Add(error);

        if (SelectedError != null && !FilteredErrors.Contains(SelectedError)) SelectedError = null;
    }

    [RelayCommand] private void NextError() { if (FilteredErrors.Count == 0) return; var i = SelectedError == null ? -1 : FilteredErrors.IndexOf(SelectedError); SelectedError = FilteredErrors[(i + 1) % FilteredErrors.Count]; }
    [RelayCommand] private void PreviousError() { if (FilteredErrors.Count == 0) return; var i = SelectedError == null ? 0 : FilteredErrors.IndexOf(SelectedError); i = (i - 1 + FilteredErrors.Count) % FilteredErrors.Count; SelectedError = FilteredErrors[i]; }

    [RelayCommand]
    private async Task SaveDocumentAsync()
    {
        var path = FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            if (_fileDialogService == null) return;
            path = await _fileDialogService.PickTextFileToSaveAsync(FileName);
            if (string.IsNullOrWhiteSpace(path)) return;
        }
        try
        {
            CheckStatus = "Сохранение...";
            await _fileService.SaveTextAsync(path, InputText, FilePath, _fixedErrors);
            FilePath = path;
            FileName = Path.GetFileName(path);
            CheckStatus = "Документ сохранён";
        }
        catch (Exception ex) { CheckStatus = $"Ошибка сохранения: {ex.Message}"; }
    }

    // НОВАЯ КОМАНДА: Экспорт файла
    [RelayCommand]
    private async Task ExportFileAsync(string format)
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        if (_fileDialogService == null) return;

        try
        {
            var defaultName = Path.GetFileNameWithoutExtension(FileName) + "_export";
            var path = await _fileDialogService.PickTextFileToSaveAsync(defaultName);

            if (string.IsNullOrWhiteSpace(path)) return;

            var extension = format.ToLowerInvariant();
            if (!path.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
            {
                path = Path.ChangeExtension(path, extension);
            }

            CheckStatus = $"Экспорт в {extension.ToUpper()}...";

            if (extension == "html")
            {
                var htmlContent = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>{WebUtility.HtmlEncode(FileName)}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; padding: 20px; color: #333; background: #fff; }}
        pre {{ white-space: pre-wrap; font-family: inherit; font-size: 16px; }}
    </style>
</head>
<body>
    <pre>{WebUtility.HtmlEncode(InputText)}</pre>
</body>
</html>";
                await File.WriteAllTextAsync(path, htmlContent);
            }
            else if (extension == "pdf")
            {
                // Настройка лицензии (обязательно для QuestPDF)
                QuestPDF.Settings.License = LicenseType.Community;

                // Генерируем PDF
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial")); // Шрифт с поддержкой кириллицы

                        page.Content().Column(col =>
                        {
                            col.Item().Text(InputText);
                        });
                    });
                }).GeneratePdf(path);
            }
            else
            {
                await _fileService.SaveTextAsync(path, InputText, FilePath, _fixedErrors);
            }

            CheckStatus = $"Успешно экспортировано в {extension.ToUpper()}";
        }
        catch (Exception ex)
        {
            CheckStatus = $"Ошибка экспорта: {ex.Message}";
        }
    }
}