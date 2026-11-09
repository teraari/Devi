using System.Linq;
using System.Threading.Tasks;
using Proofreader.Core.Models;
using Proofreader.Language.Grammar;
using Xunit;

namespace Proofreader.Tests;

public class ContextAnalyzerTests
{
    private readonly LocalGrammarChecker _checker = new();

    [Fact]
    public async Task Context_AmbiguousPunctuation()
    {
        const string text = "Казнить нельзя помиловать";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Style, error.ErrorType);
        Assert.Equal("Смысловая неоднозначность", error.ShortMessage);
        Assert.Contains("Казнить, нельзя помиловать", error.Suggestions);
        Assert.Contains("Казнить нельзя, помиловать", error.Suggestions);
    }

    [Fact]
    public async Task Context_Paronomasia_OdelPalto()
    {
        const string text = "Он одел пальто";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Style, error.ErrorType);
        Assert.Equal("Пароним", error.ShortMessage);
        Assert.Contains("надел", error.Suggestions);
    }

    [Fact]
    public async Task Context_Paronomasia_NadelRebenka()
    {
        const string text = "Она надела ребенка";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Style, error.ErrorType);
        Assert.Equal("Пароним", error.ShortMessage);
        Assert.Contains("одела", error.Suggestions);
    }

    [Fact]
    public async Task Context_Pleonasm_PamyatnySuvenir()
    {
        const string text = "Это памятный сувенир";
        var errors = await _checker.CheckGrammarAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(ErrorType.Style, error.ErrorType);
        Assert.Equal("Плеоназм", error.ShortMessage);
        Assert.Contains("сувенир", error.Suggestions);
    }

    [Fact]
    public async Task Context_OpłatitZa()
    {
        const string text = "Я хочу оплатить за проезд";
        var errors = await _checker.CheckGrammarAsync(text);

        // Может быть 2 ошибки: "оплатить за" и синтаксис. Ищем именно нашу.
        var error = Assert.Single(errors, e => e.ShortMessage == "Речевая ошибка");
        Assert.Contains("оплатить", error.Suggestions);
    }

    [Fact]
    public async Task Context_CorrectText_NoErrors()
    {
        const string text = "Он надел пальто и одел ребенка. Это просто сувенир.";
        var errors = await _checker.CheckGrammarAsync(text);

        Assert.Empty(errors);
    }
}