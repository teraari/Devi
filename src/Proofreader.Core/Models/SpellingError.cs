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

    // КОМАНДА: Принять исправление (вызывается из UI при клике на слово)
    public ICommand? ApplySuggestionCommand { get; set; }
}