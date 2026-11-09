using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Proofreader.Core.Models;
using Proofreader.Language.Morphology;
using Proofreader.Language.Spelling;

namespace Proofreader.Language.Context;

public class ContextAnalyzer
{
    private readonly TextTokenizer _tokenizer;
    private readonly MorphologicalAnalyzer _analyzer;

    private static readonly HashSet<string> Clothes = new(StringComparer.OrdinalIgnoreCase)
    {
        "пальто", "шапка", "куртка", "платье", "костюм", "брюки", "джинсы", "свитер", "рубашка", "обувь"
    };

    // ДОБАВЛЕНО: падежные формы
    private static readonly HashSet<string> AnimateObjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "ребенок", "ребенка", "ребенку", "ребенком", "ребенке",
        "кукла", "куклу", "брат", "брата", "сестра", "сестру", "сын", "сына", "дочь", "дочь", 
        "мальчик", "мальчика", "девочка", "девочку", "собака", "собаку", "кошка", "кошку"
    };

    private static readonly Dictionary<string, (string Replacement, string Explanation)> Pleonasms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "памятный сувенир", ("сувенир", "«Сувенир» — это и есть памятная вещь. Слово «памятный» — лишнее.") },
        { "свободная вакансия", ("вакансия", "«Вакансия» — это уже свободная должность. Слово «свободная» — лишнее.") },
        { "главный лидер", ("лидер", "«Лидер» — уже главный. Слово «главный» — лишнее.") },
        { "моя автобиография", ("автобиография", "«Автобиография» — это описание своей жизни. Слово «моя» — лишнее.") }
    };

    public ContextAnalyzer()
    {
        _tokenizer = new TextTokenizer();
        _analyzer = new MorphologicalAnalyzer();
    }

    public List<GrammarError> CheckContext(string text, CancellationToken cancellationToken = default)
    {
        var errors = new List<GrammarError>();
        if (string.IsNullOrEmpty(text)) return errors;

        var tokens = _tokenizer.Tokenize(text).ToList();

        // 1. Проверка на смысловую неоднозначность
        for (int i = 0; i < tokens.Count - 2; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var t1 = tokens[i];
            var t2 = tokens[i + 1];
            var t3 = tokens[i + 2];

            var info1 = _analyzer.Analyze(t1.Word);
            var info2 = _analyzer.Analyze(t2.Word);
            var info3 = _analyzer.Analyze(t3.Word);

            if (info1.PartOfSpeech == PartOfSpeech.Verb && 
                t2.Word.Equals("нельзя", StringComparison.OrdinalIgnoreCase) && 
                info3.PartOfSpeech == PartOfSpeech.Verb)
            {
                int between1 = t2.Start - (t1.Start + t1.Length);
                int between2 = t3.Start - (t2.Start + t2.Length);
                string gap1 = text.Substring(t1.Start + t1.Length, between1);
                string gap2 = text.Substring(t2.Start + t2.Length, between2);

                if (!gap1.Contains(",") && !gap1.Contains("—") && !gap2.Contains(",") && !gap2.Contains("—"))
                {
                    errors.Add(new GrammarError
                    {
                        StartIndex = t1.Start,
                        Length = t3.Start + t3.Length - t1.Start,
                        OriginalText = text.Substring(t1.Start, t3.Start + t3.Length - t1.Start),
                        Message = "Двусмысленность: непонятно, к чему относится «нельзя». Поставьте запятую или тире, чтобы прояснить смысл.",
                        ShortMessage = "Смысловая неоднозначность",
                        Suggestions = new List<string> 
                        { 
                            $"{t1.Word}, {t2.Word} {t3.Word}", 
                            $"{t1.Word} {t2.Word}, {t3.Word}",
                            $"{t1.Word} — {t2.Word} {t3.Word}"
                        },
                        ErrorType = ErrorType.Style
                    });
                }
            }
        }

        // 2. Смысловые паронимы: Одеть / Надеть
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var t1 = tokens[i];
            var t2 = tokens[i + 1];
            var info1 = _analyzer.Analyze(t1.Word);

            if (info1.PartOfSpeech == PartOfSpeech.Verb)
            {
                var lower1 = t1.Word.ToLowerInvariant();
                var lower2 = t2.Word.ToLowerInvariant();
                
                if ((lower1.StartsWith("одел") || lower1.StartsWith("оден")) && Clothes.Contains(lower2))
                {
                    var replacement = t1.Word.Replace("одел", "надел").Replace("оден", "наден");
                    errors.Add(new GrammarError
                    {
                        StartIndex = t1.Start,
                        Length = t1.Length,
                        OriginalText = t1.Word,
                        Message = "«Надеть» — что-то на себя. «Одеть» — кого-то другого. В данном контексте правильно «надеть».",
                        ShortMessage = "Пароним",
                        Suggestions = new List<string> { replacement },
                        ErrorType = ErrorType.Style
                    });
                }
                
                if ((lower1.StartsWith("надел") || lower1.StartsWith("наден")) && AnimateObjects.Contains(lower2))
                {
                    var replacement = t1.Word.Replace("надел", "одел").Replace("наден", "оден");
                    errors.Add(new GrammarError
                    {
                        StartIndex = t1.Start,
                        Length = t1.Length,
                        OriginalText = t1.Word,
                        Message = "«Одеть» — кого-то другого. «Надеть» — что-то на себя. В данном контексте правильно «одеть».",
                        ShortMessage = "Пароним",
                        Suggestions = new List<string> { replacement },
                        ErrorType = ErrorType.Style
                    });
                }
            }
        }

        // 3. Плеоназмы (смысловые излишества)
        foreach (var rule in Pleonasms)
        {
            var index = text.IndexOf(rule.Key, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                errors.Add(new GrammarError
                {
                    StartIndex = index,
                    Length = rule.Key.Length,
                    OriginalText = text.Substring(index, rule.Key.Length),
                    Message = rule.Value.Explanation,
                    ShortMessage = "Плеоназм",
                    Suggestions = new List<string> { rule.Value.Replacement },
                    ErrorType = ErrorType.Style
                });

                index = text.IndexOf(rule.Key, index + rule.Key.Length, StringComparison.OrdinalIgnoreCase);
            }
        }

        return errors;
    }
}