using System.Linq;
using System.Threading.Tasks;
using Proofreader.Core.Models;
using Proofreader.Language.Grammar;
using Xunit;

namespace Proofreader.Tests;

public class LocalGrammarCheckerTests
{
    private readonly LocalGrammarChecker _checker = new();

    [Fact]
    public async Task AdjectiveNounAgreement_GenderMismatch()
    {
        const string text = "красивый девушка";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal("Согласование", error.ShortMessage);
        Assert.Contains("красивая", error.Suggestions);
    }

    [Fact]
    public async Task AdjectiveNounAgreement_NumberMismatch()
    {
        const string text = "новый книги";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Contains("новые", error.Suggestions);
    }

    [Fact]
    public async Task NounVerbAgreement_GenderMismatch()
    {
        const string text = "девушка читал";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Contains("читала", error.Suggestions);
    }

    [Fact]
    public async Task NounVerbAgreement_NumberMismatch()
    {
        const string text = "дети бежит";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Contains("бегут", error.Suggestions);
    }

    [Fact]
    public async Task PhraseRule_SkuchayuZa()
    {
        const string text = "Я скучаю за тобой";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Contains("скучаю по", error.Suggestions);
    }

    [Fact]
    public async Task PhraseRule_OdelPalto()
    {
        const string text = "Он одел пальто";
        var errors = await _checker.CheckGrammarAsync(text);

        // ИСПРАВЛЕНО: логика "одел пальто" теперь в ContextAnalyzer, 
        // и он предлагает только замену самого глагола на "надел"
        var error = Assert.Single(errors);
        Assert.Contains("надел", error.Suggestions);
    }

    [Fact]
    public async Task CorrectText_ReturnsNoErrors()
    {
        const string text = "Красивая девушка читала интересную книгу.";
        var errors = await _checker.CheckGrammarAsync(text);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Punctuation_MissingCommaBeforeConjunction()
    {
        const string text = "Я знал что ты придешь";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Punctuation, error.ErrorType);
        Assert.Contains(", что", error.Suggestions);
    }

    [Fact]
    public async Task Punctuation_MissingCommaAfterIntroductoryWord()
    {
        const string text = "Конечно он придет";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Punctuation, error.ErrorType);
        Assert.Contains("Конечно,", error.Suggestions);
    }

    [Fact]
    public async Task Punctuation_MissingDashBetweenSubjectAndPredicate()
    {
        const string text = "Москва столица";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Punctuation, error.ErrorType);
        Assert.Contains("Москва — столица", error.Suggestions);
    }
}