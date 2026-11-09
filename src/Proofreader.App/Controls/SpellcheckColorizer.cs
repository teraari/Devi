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

    private static readonly IBrush SpellingBrush = Brushes.Red;
    private static readonly IBrush PunctuationBrush = Brushes.Orange;
    private static readonly IBrush GrammarBrush = Brushes.Gold;
    private static readonly IBrush StyleBrush = Brushes.DodgerBlue;

    private static readonly IBrush AutoFixedBrush = Brushes.LimeGreen; 
    private static readonly IBrush LowConfidenceBrush = Brushes.Yellow; 

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
                    IBrush brush;
                    if (error.IsAutoFixed) 
                        brush = AutoFixedBrush;
                    else if (error.Confidence == ErrorConfidence.Low) 
                        brush = LowConfidenceBrush;
                    else
                    {
                        brush = error.ErrorType switch
                        {
                            ErrorType.Spelling => SpellingBrush,
                            ErrorType.Punctuation => PunctuationBrush,
                            ErrorType.Grammar => GrammarBrush,
                            ErrorType.Style => StyleBrush,
                            _ => SpellingBrush
                        };
                    }

                    var decoration = new TextDecoration
                    {
                        Location = TextDecorationLocation.Underline,
                        Stroke = brush,
                        StrokeThickness = 2.0
                    };

                    element.TextRunProperties.SetTextDecorations(new TextDecorationCollection { decoration });
                });
            }
        }
    }
}