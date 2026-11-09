namespace Proofreader.Language.Morphology;

public enum PartOfSpeech
{
    Unknown,
    Noun,        // Существительное
    Adjective,   // Прилагательное
    Verb,        // Глагол
    Adverb,      // Наречие
    Pronoun,     // Местоимение
    Preposition, // Предлог
    Conjunction, // Союз
    Particle,    // Частица
    Numeral,     // Числительное
    Participle,  // Причастие
    Gerund       // Деепричастие
}

public enum Gender
{
    Unknown,
    Masculine, // Мужской
    Feminine,  // Женский
    Neuter     // Средний
}

public enum Number
{
    Unknown,
    Singular, // Единственное
    Plural    // Множественное
}

public enum Case
{
    Unknown,
    Nominative,     // Именительный (кто? что?)
    Genitive,       // Родительный (кого? чего?)
    Dative,         // Дательный (кому? чему?)
    Accusative,     // Винительный (кого? что?)
    Instrumental,   // Творительный (кем? чем?)
    Prepositional   // Предложный (о ком? о чём?)
}

/// <summary>
/// Результат морфологического разбора слова.
/// </summary>
public record MorphologicalInfo(
    PartOfSpeech PartOfSpeech,
    Gender Gender,
    Number Number,
    Case Case,
    string Lemma // Начальная форма (например, для "котами" -> "кот")
);