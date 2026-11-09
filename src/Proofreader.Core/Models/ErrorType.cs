namespace Proofreader.Core.Models;

public enum ErrorType
{
    Spelling,    // Орфография (красный)
    Punctuation, // Пунктуация (оранжевый)
    Grammar,     // Грамматика (желтый)
    Style        // Стилистика (синий)
}