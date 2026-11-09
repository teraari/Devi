using System;
using Proofreader.Language.Morphology;
using Xunit;

namespace Proofreader.Tests;

public class MorphologicalAnalyzerTests
{
    private readonly MorphologicalAnalyzer _analyzer = new();

    [Fact]
    public void Noun_Masculine_Singular()
    {
        var info = _analyzer.Analyze("дом");
        Assert.Equal(PartOfSpeech.Noun, info.PartOfSpeech);
        Assert.Equal(Gender.Masculine, info.Gender);
        Assert.Equal(Number.Singular, info.Number);
        Assert.Equal(Case.Nominative, info.Case);
    }

    [Fact]
    public void Noun_Feminine_Singular()
    {
        var info = _analyzer.Analyze("книга");
        Assert.Equal(PartOfSpeech.Noun, info.PartOfSpeech);
        Assert.Equal(Gender.Feminine, info.Gender);
        Assert.Equal(Number.Singular, info.Number);
        Assert.Equal(Case.Nominative, info.Case);
    }

    [Fact]
    public void Adjective_Feminine_Singular()
    {
        var info = _analyzer.Analyze("красивая");
        Assert.Equal(PartOfSpeech.Adjective, info.PartOfSpeech);
        Assert.Equal(Gender.Feminine, info.Gender);
        Assert.Equal(Number.Singular, info.Number);
        Assert.Equal(Case.Nominative, info.Case);
    }

    [Fact]
    public void Verb_PastTense_Feminine()
    {
        var info = _analyzer.Analyze("читала");
        Assert.Equal(PartOfSpeech.Verb, info.PartOfSpeech);
        Assert.Equal(Gender.Feminine, info.Gender);
        Assert.Equal(Number.Singular, info.Number);
    }

    [Fact]
    public void Verb_Infinitive()
    {
        var info = _analyzer.Analyze("бежать");
        Assert.Equal(PartOfSpeech.Verb, info.PartOfSpeech);
    }

    [Fact]
    public void Preposition_IsRecognized()
    {
        var info = _analyzer.Analyze("в");
        Assert.Equal(PartOfSpeech.Preposition, info.PartOfSpeech);
    }

    [Fact]
    public void Pronoun_IsRecognized()
    {
        var info = _analyzer.Analyze("который");
        Assert.Equal(PartOfSpeech.Pronoun, info.PartOfSpeech);
    }

    [Fact]
    public void Noun_Prepositional_Case()
    {
        var info = _analyzer.Analyze("доме"); // о доме
        Assert.Equal(PartOfSpeech.Noun, info.PartOfSpeech);
    }
}