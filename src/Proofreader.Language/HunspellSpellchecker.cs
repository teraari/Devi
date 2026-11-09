using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;
using Proofreader.Language.Spelling;
using WeCantSpell.Hunspell;

namespace Proofreader.Language;

public class HunspellSpellchecker : ISpellchecker
{
    private readonly WordList _wordList;
    private readonly IDictionaryService _customDictionary;
    private readonly TextTokenizer _tokenizer;
    private readonly SuggestionEngine _suggestionEngine;

    public HunspellSpellchecker(IDictionaryService customDictionary)
    {
        _customDictionary = customDictionary
            ?? throw new ArgumentNullException(nameof(customDictionary));

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dicPath = Path.Combine(baseDir, "Dictionaries", "ru_RU.dic");
        var affPath = Path.Combine(baseDir, "Dictionaries", "ru_RU.aff");

        if (!File.Exists(dicPath))
            throw new FileNotFoundException($"Словарь не найден по пути: {dicPath}", dicPath);

        if (!File.Exists(affPath))
            throw new FileNotFoundException($"Файл аффиксов не найден по пути: {affPath}", affPath);

        _wordList = WordList.CreateFromFiles(dicPath);
        
        _tokenizer = new TextTokenizer();
        _suggestionEngine = new SuggestionEngine(_wordList);
    }

    public Task<IReadOnlyList<SpellingError>> CheckTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (text is null)
        {
            return Task.FromResult<IReadOnlyList<SpellingError>>(
                Array.Empty<SpellingError>());
        }

        return Task.Run<IReadOnlyList<SpellingError>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (text.Length == 0)
                return Array.Empty<SpellingError>();

            var errors = new List<SpellingError>();
            var tokens = _tokenizer.Tokenize(text);

            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_customDictionary.IsWordKnown(token.Word))
                    continue;

                if (_wordList.Check(token.Word))
                    continue;

                var suggestions = _suggestionEngine.GetSuggestions(token.Word, 5);

                errors.Add(new SpellingError
                {
                    Word = token.Word,
                    StartIndex = token.Start,
                    Length = token.Length,
                    Suggestions = suggestions,
                    ErrorType = ErrorType.Spelling,
                    Confidence = ErrorConfidence.High // <--- ДОБАВЛЕНО
                });
            }

            return errors;
        }, cancellationToken);
    }
}