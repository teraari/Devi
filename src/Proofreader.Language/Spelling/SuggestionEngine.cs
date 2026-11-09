using System;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell;

namespace Proofreader.Language.Spelling;

public class SuggestionEngine
{
    private readonly WordList _wordList;

    public SuggestionEngine(WordList wordList)
    {
        _wordList = wordList ?? throw new ArgumentNullException(nameof(wordList));
    }

    // ИЗМЕНЕНО: возвращаемый тип теперь List<string> вместо IReadOnlyList<string>
    public List<string> GetSuggestions(string word, int maxSuggestions = 5)
    {
        var suggestions = new List<string>();

        // 1. Проверяем раскладку (актуально только если есть латинские буквы)
        if (word.Any(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
        {
            var translated = LayoutMapper.EnToRuTranslate(word);
            
            // Если переведенное слово есть в словаре, ставим его первым в списке
            if (_wordList.Check(translated))
            {
                suggestions.Add(translated);
            }
        }

        // 2. Если набрали достаточно, выходим
        if (suggestions.Count >= maxSuggestions)
            return suggestions;

        // 3. Добавляем варианты от Hunspell
        foreach (var sug in _wordList.Suggest(word))
        {
            if (!suggestions.Contains(sug, StringComparer.OrdinalIgnoreCase))
            {
                suggestions.Add(sug);
                if (suggestions.Count >= maxSuggestions)
                    break;
            }
        }

        return suggestions;
    }
}