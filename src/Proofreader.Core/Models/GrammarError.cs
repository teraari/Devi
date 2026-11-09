using System.Collections.Generic;

namespace Proofreader.Core.Models;

public class GrammarError
{
    public string Message { get; set; } = string.Empty;
    public string ShortMessage { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public string OriginalText { get; set; } = string.Empty;
    public ErrorType ErrorType { get; set; } = ErrorType.Grammar;

    // НОВОЕ: Уровень уверенности
    public ErrorConfidence Confidence { get; set; } = ErrorConfidence.Medium;
}