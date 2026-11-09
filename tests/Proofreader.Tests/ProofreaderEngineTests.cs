using System;
using System.Linq;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;
using Proofreader.Language;
using Proofreader.Language.Grammar;
using Xunit;

namespace Proofreader.Tests;

public class ProofreaderEngineTests
{
    private readonly ProofreaderEngine _engine;

    public ProofreaderEngineTests()
    {
        var dict = new EmptyDictionary();
        var spellchecker = new HunspellSpellchecker(dict);
        var grammarChecker = new LocalGrammarChecker();
        _engine = new ProofreaderEngine(spellchecker, grammarChecker);
    }

    [Fact]
    public async Task CheckDocumentAsync_ReturnsCombinedErrorsFromAllEngines()
    {
        // "карова" - орфография, "Красивый девушка" - грамматика, "памятный сувенир" - стиль
        const string text = "У меня карова. Красивый девушка. Это памятный сувенир.";
        
        var errors = await _engine.CheckDocumentAsync(text);

        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Word.Equals("карова", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Word.Equals("Красивый", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Word.Equals("памятный сувенир", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CheckDocumentAsync_EmptyText_ReturnsEmpty()
    {
        var errors = await _engine.CheckDocumentAsync("");
        Assert.Empty(errors);
    }

    private sealed class EmptyDictionary : IDictionaryService
    {
        public bool IsWordKnown(string word) => false;
        public Task AddWordAsync(string word) => Task.CompletedTask;
    }
}