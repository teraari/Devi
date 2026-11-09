using System;
using System.Collections.Generic;
using System.Linq;

namespace Proofreader.Language.Morphology;

public class MorphologicalAnalyzer
{
    private static readonly HashSet<string> Prepositions = new(StringComparer.OrdinalIgnoreCase)
    {
        "в", "без", "до", "из", "к", "на", "по", "о", "об", "от", "перед", "при", "с", "у", "через", "между", "за", "под", "над", "для", "про", "ради", "сквозь", "среди", "возле", "около"
    };

    private static readonly HashSet<string> Conjunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "и", "а", "но", "да", "или", "либо", "что", "чтобы", "если", "когда", "пока", "как", "будто", "словно", "потому", "поэтому", "так"
    };

    private static readonly HashSet<string> Pronouns = new(StringComparer.OrdinalIgnoreCase)
    {
        "я", "ты", "он", "она", "оно", "мы", "вы", "они", "меня", "тебя", "его", "её", "нас", "вас", "их", "мне", "тебе", "ему", "ей", "нам", "вам", "им", "мной", "тобой", "нами", "вами", "ими", "этот", "тот", "такой", "весь", "все", "каждый", "любой", "свой", "мой", "твой", "наш", "ваш", "который", "которая", "которые",
        "это" // ДОБАВЛЕНО
    };

    private static readonly HashSet<string> Particles = new(StringComparer.OrdinalIgnoreCase)
    {
        "не", "ни", "же", "ли", "бы", "ведь", "вот", "вон", "даже", "именно", "только", "лишь", "уж", "как", "будто"
    };

    private static readonly HashSet<string> Adverbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "просто", "очень", "быстро", "медленно", "хорошо", "плохо", "много", "мало" // ДОБАВЛЕНО
    };

    private static readonly HashSet<string> SingularVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "бежит", "идет", "летит", "плывет", "говорит", "читает", "пишет", "поет", "живет", "стоит", "лежит", "сидит", "висит", "смотрит", "видит", "слышит", "делает", "знает"
    };

    private static readonly HashSet<string> PluralVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "бегут", "идут", "летят", "плывут", "говорят", "читают", "пишут", "поют", "живут", "стоят", "лежат", "сидят", "висят", "смотрят", "видят", "слышат", "делают", "знают"
    };

    private static readonly HashSet<string> Gerunds = new(StringComparer.OrdinalIgnoreCase)
    {
        "читая", "решая", "гуляя", "смотря", "любя", "держа", "слыша", "идя", "неся", "везя", "плывя", "бежа"
    };

    public MorphologicalInfo Analyze(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return new MorphologicalInfo(PartOfSpeech.Unknown, Gender.Unknown, Number.Unknown, Case.Unknown, word);

        var lower = word.ToLowerInvariant();

        // 1. Закрытые классы
        if (Prepositions.Contains(lower))
            return new MorphologicalInfo(PartOfSpeech.Preposition, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        if (Conjunctions.Contains(lower))
            return new MorphologicalInfo(PartOfSpeech.Conjunction, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        if (Pronouns.Contains(lower))
            return new MorphologicalInfo(PartOfSpeech.Pronoun, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        if (Particles.Contains(lower))
            return new MorphologicalInfo(PartOfSpeech.Particle, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        if (Adverbs.Contains(lower)) // ДОБАВЛЕНО
            return new MorphologicalInfo(PartOfSpeech.Adverb, Gender.Unknown, Number.Unknown, Case.Unknown, lower);

        // 2. Глаголы настоящего времени (по словарю)
        if (SingularVerbs.Contains(lower))
            return new MorphologicalInfo(PartOfSpeech.Verb, Gender.Unknown, Number.Singular, Case.Unknown, lower);
        if (PluralVerbs.Contains(lower))
            return new MorphologicalInfo(PartOfSpeech.Verb, Gender.Unknown, Number.Plural, Case.Unknown, lower);

        // 3. Деепричастия
        if (Gerunds.Contains(lower))
            return new MorphologicalInfo(PartOfSpeech.Gerund, Gender.Unknown, Number.Unknown, Case.Unknown, lower);

        if (lower.EndsWith("в") || lower.EndsWith("вши") || lower.EndsWith("вшись"))
        {
            if (lower.Length > 4)
                return new MorphologicalInfo(PartOfSpeech.Gerund, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        }
        if (lower.EndsWith("я") && !lower.EndsWith("ая") && !lower.EndsWith("яя") && !lower.EndsWith("оя") && !lower.EndsWith("ея"))
        {
            if (lower.Length > 4 && "ауи".Contains(lower[lower.Length - 2]))
                return new MorphologicalInfo(PartOfSpeech.Gerund, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        }

        // 4. Инфинитив
        if (lower.EndsWith("ть") || lower.EndsWith("ться") || lower.EndsWith("чь") || lower.EndsWith("чься"))
            return new MorphologicalInfo(PartOfSpeech.Verb, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        
        if (lower.EndsWith("ти"))
        {
            if (lower.Length > 2 && !"аеёиоуыэюя".Contains(lower[lower.Length - 3]))
                return new MorphologicalInfo(PartOfSpeech.Verb, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
        }

        // 5. Глаголы прошедшего времени
        if (lower.EndsWith("л") || lower.EndsWith("ла") || lower.EndsWith("ло") || lower.EndsWith("ли"))
        {
            int suffixLen = lower.EndsWith("л") ? 1 : 2;
            if (lower.Length > suffixLen)
            {
                var charBeforeL = lower[lower.Length - suffixLen - 1];
                if ("аеёиуыэюя".Contains(charBeforeL))
                {
                    var gender = lower.EndsWith("ла") ? Gender.Feminine : lower.EndsWith("ло") ? Gender.Neuter : Gender.Masculine;
                    var number = lower.EndsWith("ли") ? Number.Plural : Number.Singular;
                    return new MorphologicalInfo(PartOfSpeech.Verb, gender, number, Case.Unknown, lower);
                }
            }
        }

        // 6. Прилагательные
        if (lower.EndsWith("ый") || lower.EndsWith("ий") || lower.EndsWith("ой"))
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Masculine, Number.Singular, Case.Nominative, lower.Substring(0, lower.Length - 2) + "ый");
        if (lower.EndsWith("ая") || lower.EndsWith("яя"))
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Feminine, Number.Singular, Case.Nominative, lower.Substring(0, lower.Length - 2) + "ый");
        if (lower.EndsWith("ое") || lower.EndsWith("ее"))
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Neuter, Number.Singular, Case.Nominative, lower.Substring(0, lower.Length - 2) + "ый");
        if (lower.EndsWith("ые") || lower.EndsWith("ие"))
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Unknown, Number.Plural, Case.Nominative, lower.Substring(0, lower.Length - 2) + "ый");
        if (lower.EndsWith("ого") || lower.EndsWith("его"))
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Masculine, Number.Singular, Case.Genitive, lower.Substring(0, lower.Length - 3) + "ый");
        if (lower.EndsWith("ому") || lower.EndsWith("ему"))
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Masculine, Number.Singular, Case.Dative, lower.Substring(0, lower.Length - 3) + "ый");
        if (lower.EndsWith("ым") || lower.EndsWith("им"))
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Masculine, Number.Singular, Case.Instrumental, lower.Substring(0, lower.Length - 2) + "ый");
        
        if (lower.EndsWith("ом") || lower.EndsWith("ем"))
        {
            if (lower.Length <= 4)
                return new MorphologicalInfo(PartOfSpeech.Noun, Gender.Masculine, Number.Singular, Case.Nominative, lower);
            return new MorphologicalInfo(PartOfSpeech.Adjective, Gender.Masculine, Number.Singular, Case.Prepositional, lower.Substring(0, lower.Length - 2) + "ый");
        }

        // 7. Существительные
        if (lower.EndsWith("а") || lower.EndsWith("я"))
            return new MorphologicalInfo(PartOfSpeech.Noun, Gender.Feminine, Number.Singular, Case.Nominative, lower);
        if (lower.EndsWith("ы") || lower.EndsWith("и"))
            return new MorphologicalInfo(PartOfSpeech.Noun, Gender.Unknown, Number.Plural, Case.Nominative, lower);
        if (lower.EndsWith("у") || lower.EndsWith("ю"))
            return new MorphologicalInfo(PartOfSpeech.Noun, Gender.Masculine, Number.Singular, Case.Dative, lower);
        
        if (lower.EndsWith("о") || lower.EndsWith("е"))
        {
            if (lower.Length > 2 && !lower.EndsWith("ое") && !lower.EndsWith("ее"))
                return new MorphologicalInfo(PartOfSpeech.Noun, Gender.Neuter, Number.Singular, Case.Nominative, lower);
        }

        if (lower.Length > 1 && !"аеёиоуыэюя".Contains(lower.Last()))
            return new MorphologicalInfo(PartOfSpeech.Noun, Gender.Masculine, Number.Singular, Case.Nominative, lower);

        // 8. Наречия
        if (lower.EndsWith("о") || lower.EndsWith("е"))
            return new MorphologicalInfo(PartOfSpeech.Adverb, Gender.Unknown, Number.Unknown, Case.Unknown, lower);

        return new MorphologicalInfo(PartOfSpeech.Unknown, Gender.Unknown, Number.Unknown, Case.Unknown, lower);
    }
}