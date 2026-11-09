using System.Collections.Generic;
using System.Windows.Input;

namespace Proofreader.Core.Models;

public class SpellingError
{
    public string Word { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public List<string> Suggestions { get; set; } = new();

    public string? AppliedSuggestion { get; set; }
    public ErrorType ErrorType { get; set; } = ErrorType.Spelling;

    // НОВОЕ: Уровень уверенности
    public ErrorConfidence Confidence { get; set; } = ErrorConfidence.Medium;

    // НОВОЕ: Флаг, было ли это исправлено автоматически
    public bool IsAutoFixed { get; set; } = false;

    // Команды
    public ICommand? ApplySuggestionCommand { get; set; }
    public ICommand? AddToDictionaryCommand { get; set; }
}