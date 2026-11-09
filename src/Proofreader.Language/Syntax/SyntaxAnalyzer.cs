using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Proofreader.Core.Models;
using Proofreader.Language.Morphology;
using Proofreader.Language.Spelling;

namespace Proofreader.Language.Syntax;

public class SyntaxAnalyzer
{
    private readonly TextTokenizer _tokenizer;
    private readonly MorphologicalAnalyzer _analyzer;

    public SyntaxAnalyzer()
    {
        _tokenizer = new TextTokenizer();
        _analyzer = new MorphologicalAnalyzer();
    }

    public List<GrammarError> CheckSyntax(string text, CancellationToken cancellationToken = default)
    {
        var errors = new List<GrammarError>();
        if (string.IsNullOrEmpty(text)) return errors;

        var tokens = _tokenizer.Tokenize(text).ToList();

        for (int i = 0; i < tokens.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = tokens[i];
            var info = _analyzer.Analyze(token.Word);

            // 1. Деепричастные обороты
            if (info.PartOfSpeech == PartOfSpeech.Gerund)
            {
                bool hasCommaBefore = HasCommaBefore(text, token, tokens, i);
                bool hasCommaAfter = HasCommaAfter(text, token, tokens, i);

                // В начале предложения (или после точки)
                bool isStartOfSentence = i == 0 || 
                    text.Substring(0, token.Start).TrimEnd().EndsWith(".") || 
                    text.Substring(0, token.Start).TrimEnd().EndsWith("!") ||
                    text.Substring(0, token.Start).TrimEnd().EndsWith("?");

                if (isStartOfSentence)
                {
                    if (!hasCommaAfter)
                    {
                        errors.Add(CreateError(token, token.Word, "Деепричастный оборот в начале предложения выделяется запятой.", token.Word + ","));
                    }
                }
                else if (!hasCommaBefore && !hasCommaAfter)
                {
                    // В середине предложения (выделяется с двух сторон, мы предлагаем хотя бы первую)
                    errors.Add(CreateError(token, token.Word, "Деепричастный оборот выделяется запятыми.", ", " + token.Word));
                }
            }

            // 2. Однородные члены предложения
            if (i < tokens.Count - 1)
            {
                var t1 = tokens[i];
                var t2 = tokens[i + 1];
                var info1 = _analyzer.Analyze(t1.Word);
                var info2 = _analyzer.Analyze(t2.Word);

                bool isHomogeneous = false;

                // Прилагательное + Прилагательное (например, "красный синий")
                if (info1.PartOfSpeech == PartOfSpeech.Adjective && info2.PartOfSpeech == PartOfSpeech.Adjective)
                {
                    isHomogeneous = true;
                }
                // Существительное + Существительное во множественном числе (например, "яблоки груши")
                else if (info1.PartOfSpeech == PartOfSpeech.Noun && info2.PartOfSpeech == PartOfSpeech.Noun &&
                         info1.Number == Number.Plural && info2.Number == Number.Plural)
                {
                    isHomogeneous = true;
                }

                if (isHomogeneous)
                {
                    int between = t2.Start - (t1.Start + t1.Length);
                    string gap = text.Substring(t1.Start + t1.Length, between).Trim();

                    // Если нет запятой и нет союзов (и, а, но, или, либо)
                    if (!gap.Contains(",") && 
                        gap != "и" && gap != "а" && gap != "но" && gap != "или" && gap != "либо")
                    {
                        errors.Add(CreateError(t1, t1.Word + " " + t2.Word, 
                            "Однородные члены предложения разделяются запятой.", 
                            t1.Word + ", " + t2.Word));
                    }
                }
            }
        }

        return errors;
    }

    private GrammarError CreateError(TextToken token, string original, string message, string suggestion)
    {
        return new GrammarError
        {
            StartIndex = token.Start,
            Length = token.Length,
            OriginalText = original,
            Message = message,
            ShortMessage = "Синтаксис",
            Suggestions = new List<string> { suggestion },
            ErrorType = ErrorType.Punctuation // Используем тип "Пунктуация" для UI
        };
    }

    private bool HasCommaBefore(string text, TextToken token, List<TextToken> tokens, int index)
    {
        if (index == 0) return false;
        var prev = tokens[index - 1];
        int between = token.Start - (prev.Start + prev.Length);
        return text.Substring(prev.Start + prev.Length, between).Contains(",");
    }

    private bool HasCommaAfter(string text, TextToken token, List<TextToken> tokens, int index)
    {
        if (index == tokens.Count - 1) return false;
        var next = tokens[index + 1];
        int between = next.Start - (token.Start + token.Length);
        return text.Substring(token.Start + token.Length, between).Contains(",");
    }
}