using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Proofreader.Core.Models;

namespace Proofreader.Language;

public class AutoCorrectionEngine
{
    public (string CorrectedText, List<SpellingError> AutoFixedErrors) ApplyFixes(
        string originalText, 
        IEnumerable<SpellingError> errors)
    {
        var autoFixedList = new List<SpellingError>();
        
        // Отбираем только те ошибки, которые можно исправлять автоматически
        // И у которых есть предложения по замене
        var errorsToFix = errors
            .Where(e => e.Confidence == ErrorConfidence.High && e.Suggestions.Any())
            .OrderByDescending(e => e.StartIndex) // ВАЖНО: идем с конца текста, чтобы не сломать индексы
            .ToList();

        if (!errorsToFix.Any())
            return (originalText, autoFixedList);

        var sb = new StringBuilder(originalText);

        foreach (var error in errorsToFix)
        {
            // Проверка на всякий случай, чтобы не выйти за границы
            if (error.StartIndex >= 0 && error.StartIndex + error.Length <= sb.Length)
            {
                var replacement = error.Suggestions.First();
                
                sb.Remove(error.StartIndex, error.Length);
                sb.Insert(error.StartIndex, replacement);

                error.IsAutoFixed = true;
                error.AppliedSuggestion = replacement;
                autoFixedList.Add(error);
            }
        }

        return (sb.ToString(), autoFixedList);
    }
}