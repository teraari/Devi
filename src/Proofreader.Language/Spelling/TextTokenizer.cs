using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Proofreader.Language.Spelling;

public record TextToken(string Word, int Start, int Length);

public class TextTokenizer
{
    // 1. Пропускаем URL и email.
    // 2. Захватываем слова, включая дефисы внутри (например, "кто-то", "по-русски").
    private static readonly Regex TokenRegex = new(
        @"(?<url>https?://\S+|www\.\S+|\S+@\S+)|(?<word>[а-яА-ЯёЁa-zA-Z]+(?:-[а-яА-ЯёЁa-zA-Z]+)*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IEnumerable<TextToken> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        foreach (Match match in TokenRegex.Matches(text))
        {
            // Если сработала группа URL — просто пропускаем
            if (match.Groups["url"].Success)
                continue;

            var wordGroup = match.Groups["word"];
            if (wordGroup.Success)
            {
                yield return new TextToken(wordGroup.Value, wordGroup.Index, wordGroup.Length);
            }
        }
    }
}