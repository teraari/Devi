using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Proofreader.Core.Models;

namespace Proofreader.App.Controls;

public class SpellcheckColorizer : DocumentColorizingTransformer
{
    public IEnumerable<SpellingError> Errors { get; set; } = Enumerable.Empty<SpellingError>();

    protected override void ColorizeLine(DocumentLine line)
    {
        var lineStart = line.Offset;
        var lineEnd = line.EndOffset;

        foreach (var error in Errors)
        {
            // Если ошибка попадает на текущую строку
            if (error.StartIndex >= lineStart && (error.StartIndex + error.Length) <= lineEnd)
            {
                ChangeLinePart(error.StartIndex, error.StartIndex + error.Length, element =>
                {
                    // Рисуем красное подчеркивание
                    element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                    // Также можно сделать сам текст красным (опционально)
                    // element.TextRunProperties.SetForegroundBrush(Brushes.Red); 
                });
            }
        }
    }
}