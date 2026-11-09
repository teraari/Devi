using System.Linq;
using System.Threading.Tasks;
using Proofreader.Core.Models;
using Proofreader.Language.Grammar;
using Xunit;

namespace Proofreader.Tests;

public class SyntaxAnalyzerTests
{
    private readonly LocalGrammarChecker _checker = new();

    [Fact]
    public async Task Syntax_MissingCommaForHomogeneousAdjectives()
    {
        const string text = "Купил красный синий шарф";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Punctuation, error.ErrorType);
        Assert.Contains("красный, синий", error.Suggestions);
    }

    [Fact]
    public async Task Syntax_MissingCommaForHomogeneousNouns()
    {
        const string text = "На столе лежали яблоки груши";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Contains("яблоки, груши", error.Suggestions);
    }

    [Fact]
    public async Task Syntax_GerundPhraseNeedsComma()
    {
        const string text = "Он читая книгу пил чай";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Contains(", читая", error.Suggestions);
    }

    [Fact]
    public async Task Syntax_GerundPhraseAtStartNeedsComma()
    {
        const string text = "Читая книгу он пил чай";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Contains("Читая,", error.Suggestions);
    }

    [Fact]
    public async Task Syntax_CorrectText_NoErrors()
    {
        const string text = "Купил красный, синий шарф. Он, читая книгу, пил чай.";
        var errors = await _checker.CheckGrammarAsync(text);

        Assert.Empty(errors);
    }
}