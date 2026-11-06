using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Proofreader.Core.Models;

public class SpellingError
{
    public string Word { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public List<string> Suggestions { get; set; } = new();

    // НОВОЕ: Храним примененное исправление (null, если еще не исправлено)
    public string? AppliedSuggestion { get; set; }

    public System.Windows.Input.ICommand? ApplySuggestionCommand { get; set; }
}