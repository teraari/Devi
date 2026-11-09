using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;

namespace Proofreader.Language;

public class ProofreaderEngine : IProofreaderEngine
{
    private readonly ISpellchecker _spellchecker;
    private readonly IGrammarChecker _grammarChecker;

    public ProofreaderEngine(ISpellchecker spellchecker, IGrammarChecker grammarChecker)
    {
        _spellchecker = spellchecker ?? throw new ArgumentNullException(nameof(spellchecker));
        _grammarChecker = grammarChecker ?? throw new ArgumentNullException(nameof(grammarChecker));
    }

    public async Task<IReadOnlyList<SpellingError>> CheckDocumentAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<SpellingError>();

        // Запускаем орфографию и грамматику параллельно
        var spellingTask = _spellchecker.CheckTextAsync(text, cancellationToken);
        var grammarTask = _grammarChecker.CheckGrammarAsync(text, cancellationToken);

        // Ждем завершения обеих задач
        await Task.WhenAll(spellingTask, grammarTask);

        var errors = new List<SpellingError>(await spellingTask);
        var grammarErrors = await grammarTask;

        // Переносим грамматические/пунктуационные/смысловые ошибки в единый список
        foreach (var gError in grammarErrors)
        {
            if (gError.StartIndex < 0 || 
                gError.Length <= 0 || 
                gError.StartIndex + gError.Length > text.Length)
            {
                continue;
            }

            var error = new SpellingError
            {
                Word = string.IsNullOrEmpty(gError.OriginalText)
                    ? text.Substring(gError.StartIndex, gError.Length)
                    : gError.OriginalText,
                StartIndex = gError.StartIndex,
                Length = gError.Length,
                Suggestions = gError.Suggestions,
                ErrorType = gError.ErrorType // Сохраняем тип (Стиль, Пунктуация, Грамматика)
            };

            errors.Add(error);
        }

        // Сортируем по порядку появления в тексте для правильной навигации в UI
        return errors.OrderBy(e => e.StartIndex).ToList();
    }
}