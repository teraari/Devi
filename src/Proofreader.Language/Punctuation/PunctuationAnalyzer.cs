using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Proofreader.Core.Models;
using Proofreader.Language.Morphology;
using Proofreader.Language.Spelling;

namespace Proofreader.Language.Punctuation;

public class PunctuationAnalyzer
{
    private readonly TextTokenizer _tokenizer;
    private readonly MorphologicalAnalyzer _analyzer;

    private static readonly HashSet<string> SubordinatingConjunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "что", "чтобы", "если", "когда", "потому", "который", "которая", "которое", "которые", "которого", "которой", "которым", "котором"
    };

    private static readonly HashSet<string> IntroductoryWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "конечно", "вероятно", "возможно", "наверное", "кажется", "например", "кстати", "впрочем", "однако", "во-первых", "во-вторых", "итак", "следовательно"
    };

    public PunctuationAnalyzer()
    {
        _tokenizer = new TextTokenizer();
        _analyzer = new MorphologicalAnalyzer();
    }

    public List<GrammarError> CheckPunctuation(string text, CancellationToken cancellationToken = default)
    {
        var errors = new List<GrammarError>();
        if (string.IsNullOrEmpty(text)) return errors;

        var tokens = _tokenizer.Tokenize(text).ToList();

        for (int i = 0; i < tokens.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = tokens[i];
            var lower = token.Word.ToLowerInvariant();

            // 1. Проверка вводных слов
            if (IntroductoryWords.Contains(lower))
            {
                int nextIndex = token.Start + token.Length;
                bool hasCommaAfter = false;
                for (int j = nextIndex; j < text.Length; j++)
                {
                    if (char.IsWhiteSpace(text[j])) continue;
                    if (text[j] == ',') hasCommaAfter = true;
                    break;
                }

                if (!hasCommaAfter)
                {
                    errors.Add(new GrammarError
                    {
                        StartIndex = token.Start,
                        Length = token.Length,
                        OriginalText = token.Word,
                        Message = $"Вводное слово «{token.Word}» выделяется запятой.",
                        ShortMessage = "Пунктуация",
                        Suggestions = new List<string> { token.Word + "," },
                        ErrorType = ErrorType.Punctuation
                    });
                }
            }

            // 2. Проверка подчинительных союзов (что, чтобы, если, когда, который...)
            if (SubordinatingConjunctions.Contains(lower))
            {
                bool hasCommaBefore = false;
                if (i > 0)
                {
                    var prevToken = tokens[i - 1];
                    int between = token.Start - (prevToken.Start + prevToken.Length);
                    string gap = text.Substring(prevToken.Start + prevToken.Length, between);
                    if (gap.Contains(',')) hasCommaBefore = true;
                }

                bool isSubordinate = false;
                if (i > 0)
                {
                    var prevInfo = _analyzer.Analyze(tokens[i - 1].Word);
                    if (prevInfo.PartOfSpeech == PartOfSpeech.Verb || prevInfo.PartOfSpeech == PartOfSpeech.Noun || prevInfo.PartOfSpeech == PartOfSpeech.Adjective)
                    {
                        isSubordinate = true;
                    }
                }

                if (isSubordinate && !hasCommaBefore)
                {
                    int startIndex = token.Start;
                    int length = token.Length;
                    string originalText = token.Word;
                    string suggestion = ", " + token.Word;

                    if (startIndex > 0 && char.IsWhiteSpace(text[startIndex - 1]))
                    {
                        startIndex--;
                        length++;
                        originalText = text.Substring(startIndex, length);
                    }
                    else
                    {
                        suggestion = "," + token.Word;
                    }

                    errors.Add(new GrammarError
                    {
                        StartIndex = startIndex,
                        Length = length,
                        OriginalText = originalText,
                        Message = $"Перед союзом «{token.Word}» ставится запятая.",
                        ShortMessage = "Пунктуация",
                        Suggestions = new List<string> { suggestion },
                        ErrorType = ErrorType.Punctuation
                    });
                }
            }

            // 3. Тире между подлежащим и сказуемым (ИСПРАВЛЕНО: только единственное число)
            if (i < tokens.Count - 1)
            {
                var t1 = tokens[i];
                var t2 = tokens[i + 1];
                var info1 = _analyzer.Analyze(t1.Word);
                var info2 = _analyzer.Analyze(t2.Word);

                if (info1.PartOfSpeech == PartOfSpeech.Noun && info1.Case == Case.Nominative && info1.Number == Number.Singular &&
                    info2.PartOfSpeech == PartOfSpeech.Noun && info2.Case == Case.Nominative && info2.Number == Number.Singular)
                {
                    int between = t2.Start - (t1.Start + t1.Length);
                    string gap = text.Substring(t1.Start + t1.Length, between);
                    
                    if (gap.Trim().Length == 0) 
                    {
                        errors.Add(new GrammarError
                        {
                            StartIndex = t1.Start,
                            Length = t1.Length + between + t2.Length,
                            OriginalText = $"{t1.Word} {t2.Word}",
                            Message = "Между подлежащим и сказуемым-существительным ставится тире.",
                            ShortMessage = "Пунктуация",
                            Suggestions = new List<string> { $"{t1.Word} — {t2.Word}" },
                            ErrorType = ErrorType.Punctuation
                        });
                    }
                }
            }
        }

        return errors;
    }
}