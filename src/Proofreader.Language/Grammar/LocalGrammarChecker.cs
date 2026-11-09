using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;
using Proofreader.Language.Context;
using Proofreader.Language.Morphology;
using Proofreader.Language.Punctuation;
using Proofreader.Language.Spelling;
using Proofreader.Language.Syntax;

namespace Proofreader.Language.Grammar;

public class LocalGrammarChecker : IGrammarChecker
{
    private readonly TextTokenizer _tokenizer;
    private readonly MorphologicalAnalyzer _analyzer;
    private readonly PunctuationAnalyzer _punctuationAnalyzer;
    private readonly SyntaxAnalyzer _syntaxAnalyzer;
    private readonly ContextAnalyzer _contextAnalyzer;

    private static readonly Dictionary<string, (string Replacement, string Explanation)> PhraseRules = new(StringComparer.OrdinalIgnoreCase)
    {
        { "скучаю за", ("скучаю по", "Глагол «скучать» требует предлога «по» (скучать по кому-то), а не «за».") },
        { "скучает за", ("скучает по", "Глагол «скучать» требует предлога «по».") },
        { "скучаем за", ("скучаем по", "Глагол «скучать» требует предлога «по».") },
        { "оплатить за", ("оплатить", "Правильно: «оплатить проезд» или «заплатить за проезд».") }
    };

    public LocalGrammarChecker()
    {
        _tokenizer = new TextTokenizer();
        _analyzer = new MorphologicalAnalyzer();
        _punctuationAnalyzer = new PunctuationAnalyzer();
        _syntaxAnalyzer = new SyntaxAnalyzer();
        _contextAnalyzer = new ContextAnalyzer();
    }

    public Task<IReadOnlyList<GrammarError>> CheckGrammarAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
            return Task.FromResult<IReadOnlyList<GrammarError>>(Array.Empty<GrammarError>());

        return Task.Run<IReadOnlyList<GrammarError>>(() =>
        {
            var errors = new List<GrammarError>();

            cancellationToken.ThrowIfCancellationRequested();

            CheckPhraseRules(text, errors);

            var tokens = _tokenizer.Tokenize(text).ToList();
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var t1 = tokens[i];
                var t2 = tokens[i + 1];

                var info1 = _analyzer.Analyze(t1.Word);
                var info2 = _analyzer.Analyze(t2.Word);

                if (info1.PartOfSpeech == PartOfSpeech.Adjective && info2.PartOfSpeech == PartOfSpeech.Noun)
                {
                    CheckAdjectiveNounAgreement(t1, info1, t2, info2, errors);
                }
                else if (info1.PartOfSpeech == PartOfSpeech.Noun)
                {
                    if (info1.Case != Case.Nominative) continue;
                    if (i > 0 && _analyzer.Analyze(tokens[i - 1].Word).PartOfSpeech == PartOfSpeech.Preposition) continue;

                    bool isVerb = info2.PartOfSpeech == PartOfSpeech.Verb;

                    if (!isVerb && info2.PartOfSpeech == PartOfSpeech.Noun)
                    {
                        if (t2.Word.EndsWith("ет") || t2.Word.EndsWith("ит") || t2.Word.EndsWith("ут") || t2.Word.EndsWith("ют") || t2.Word.EndsWith("ат") || t2.Word.EndsWith("ят"))
                        {
                            isVerb = true;
                            var num = (t2.Word.EndsWith("ут") || t2.Word.EndsWith("ют") || t2.Word.EndsWith("ат") || t2.Word.EndsWith("ят")) ? Number.Plural : Number.Singular;
                            info2 = new MorphologicalInfo(PartOfSpeech.Verb, Gender.Unknown, num, Case.Unknown, t2.Word);
                        }
                    }

                    if (isVerb)
                    {
                        CheckNounVerbAgreement(t1, info1, t2, info2, errors);
                    }
                }
            }

            var punctuationErrors = _punctuationAnalyzer.CheckPunctuation(text, cancellationToken);
            errors.AddRange(punctuationErrors);

            var syntaxErrors = _syntaxAnalyzer.CheckSyntax(text, cancellationToken);
            errors.AddRange(syntaxErrors);

            var contextErrors = _contextAnalyzer.CheckContext(text, cancellationToken);
            errors.AddRange(contextErrors);

            return errors;
        }, cancellationToken);
    }

    private void CheckPhraseRules(string text, List<GrammarError> errors)
    {
        foreach (var rule in PhraseRules)
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
                    ShortMessage = "Речевая ошибка",
                    Suggestions = new List<string> { rule.Value.Replacement },
                    Confidence = ErrorConfidence.High // <--- ДОБАВЛЕНО (исправляем сразу)
                });

                index = text.IndexOf(rule.Key, index + rule.Key.Length, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private void CheckAdjectiveNounAgreement(
        TextToken t1, MorphologicalInfo i1, 
        TextToken t2, MorphologicalInfo i2, 
        List<GrammarError> errors)
    {
        bool mismatch = false;
        string explanation = "";

        if (i1.Number != Number.Unknown && i2.Number != Number.Unknown && i1.Number != i2.Number)
        {
            mismatch = true;
            explanation = $"Нарушено согласование в числе: прилагательное «{t1.Word}» ({i1.Number}) и существительное «{t2.Word}» ({i2.Number}).";
        }
        else if (i1.Number == Number.Singular && i2.Number == Number.Singular &&
                 i1.Gender != Gender.Unknown && i2.Gender != Gender.Unknown && i1.Gender != i2.Gender)
        {
            mismatch = true;
            explanation = $"Нарушено согласование в роде: прилагательное «{t1.Word}» ({i1.Gender}) и существительное «{t2.Word}» ({i2.Gender}).";
        }

        if (mismatch)
        {
            var suggestion = FixAdjectiveEnding(t1.Word, i2.Gender, i2.Number);
            
            errors.Add(new GrammarError
            {
                StartIndex = t1.Start,
                Length = t1.Length,
                OriginalText = t1.Word,
                Message = explanation,
                ShortMessage = "Согласование",
                Suggestions = string.IsNullOrEmpty(suggestion) ? new List<string>() : new List<string> { suggestion },
                Confidence = ErrorConfidence.Medium // <--- ДОБАВЛЕНО (пусть решает пользователь)
            });
        }
    }

    private void CheckNounVerbAgreement(
        TextToken t1, MorphologicalInfo i1, 
        TextToken t2, MorphologicalInfo i2, 
        List<GrammarError> errors)
    {
        bool mismatch = false;
        string explanation = "";

        if (i1.Number != Number.Unknown && i2.Number != Number.Unknown && i1.Number != i2.Number)
        {
            mismatch = true;
            explanation = $"Нарушено согласование подлежащего и сказуемого в числе: «{t1.Word}» ({i1.Number}) и «{t2.Word}» ({i2.Number}).";
        }
        else if (i1.Number == Number.Singular && i2.Number == Number.Singular && 
                 i1.Gender != Gender.Unknown && i2.Gender != Gender.Unknown && i1.Gender != i2.Gender)
        {
            mismatch = true;
            explanation = $"Нарушено согласование в роде: «{t1.Word}» ({i1.Gender}) и глагол «{t2.Word}» ({i2.Gender}).";
        }

        if (mismatch)
        {
            var suggestion = FixVerbEnding(t2.Word, i1.Gender, i1.Number);
            errors.Add(new GrammarError
            {
                StartIndex = t2.Start,
                Length = t2.Length,
                OriginalText = t2.Word,
                Message = explanation,
                ShortMessage = "Согласование",
                Suggestions = string.IsNullOrEmpty(suggestion) ? new List<string>() : new List<string> { suggestion },
                Confidence = ErrorConfidence.Medium // <--- ДОБАВЛЕНО (пусть решает пользователь)
            });
        }
    }

    private string FixAdjectiveEnding(string adj, Gender gender, Number number)
    {
        if (number == Number.Plural)
        {
            if (adj.EndsWith("ый") || adj.EndsWith("ий") || adj.EndsWith("ой")) return adj.Substring(0, adj.Length - 2) + "ые";
            if (adj.EndsWith("ая") || adj.EndsWith("яя")) return adj.Substring(0, adj.Length - 2) + "ые";
            if (adj.EndsWith("ое") || adj.EndsWith("ее")) return adj.Substring(0, adj.Length - 2) + "ые";
            return "";
        }

        var stem = adj.Length > 2 ? adj.Substring(0, adj.Length - 2) : adj;
        return gender switch
        {
            Gender.Feminine => stem + "ая",
            Gender.Neuter => stem + "ое",
            Gender.Masculine => stem + "ый",
            _ => ""
        };
    }

    private string FixVerbEnding(string verb, Gender gender, Number number)
    {
        if (gender == Gender.Unknown) 
        {
            if (number == Number.Plural)
            {
                if (verb.Equals("бежит", StringComparison.OrdinalIgnoreCase)) return "бегут";
                if (verb.Equals("идет", StringComparison.OrdinalIgnoreCase)) return "идут";
                if (verb.Equals("летит", StringComparison.OrdinalIgnoreCase)) return "летят";
                if (verb.Equals("говорит", StringComparison.OrdinalIgnoreCase)) return "говорят";
                if (verb.Equals("читает", StringComparison.OrdinalIgnoreCase)) return "читают";
                if (verb.EndsWith("ет")) return verb.Substring(0, verb.Length - 2) + "ют";
                if (verb.EndsWith("ит")) return verb.Substring(0, verb.Length - 2) + "ят";
            }
            else if (number == Number.Singular)
            {
                if (verb.Equals("бегут", StringComparison.OrdinalIgnoreCase)) return "бежит";
                if (verb.Equals("идут", StringComparison.OrdinalIgnoreCase)) return "идет";
                if (verb.Equals("летят", StringComparison.OrdinalIgnoreCase)) return "летит";
                if (verb.Equals("говорят", StringComparison.OrdinalIgnoreCase)) return "говорит";
                if (verb.Equals("читают", StringComparison.OrdinalIgnoreCase)) return "читает";
                if (verb.EndsWith("ют")) return verb.Substring(0, verb.Length - 2) + "ет";
                if (verb.EndsWith("ят")) return verb.Substring(0, verb.Length - 2) + "ит";
            }
            return "";
        }

        if (number == Number.Plural) return verb.EndsWith("л") ? verb + "и" : verb;
        if (gender == Gender.Feminine) return verb.EndsWith("л") ? verb + "а" : verb;
        if (gender == Gender.Neuter) return verb.EndsWith("л") ? verb + "о" : verb;
        return verb;
    }
}