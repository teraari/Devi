using System.Collections.Generic;

namespace Proofreader.Core.Models;

public class SpellingError
{
    // Само слово с ошибкой
    public string Word { get; set; } = string.Empty;
    
    // Позиция слова в тексте (с какого символа начинается)
    public int StartIndex { get; set; }
    
    // Длина слова
    public int Length { get; set; }
    
    // Список предлагаемых исправлений
    public List<string> Suggestions { get; set; } = new();
}