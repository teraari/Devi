using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;
using Proofreader.Language;
using Xunit;

namespace Proofreader.Tests;

public class HunspellSpellcheckerTests
{
    private static HunspellSpellchecker CreateSpellchecker(
        IDictionaryService? dictionary = null) =>
        new(dictionary ?? new EmptyDictionary());

    [Fact]
    public async Task EmptyText_ReturnsNoErrors()
    {
        var checker = CreateSpellchecker();
        var errors = await checker.CheckTextAsync(string.Empty);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task NullText_ReturnsNoErrors()
    {
        var checker = CreateSpellchecker();
        var errors = await checker.CheckTextAsync(null!);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task WellKnownRussianWord_IsNotFlagged()
    {
        var checker = CreateSpellchecker();
        var errors = await checker.CheckTextAsync("дом");
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RussianCaseForms_AreNotFlagged()
    {
        var checker = CreateSpellchecker();
        const string text = "кота коту котом коте красивая красивого красивой красивым бежит бегут бежал бежала";
        var errors = await checker.CheckTextAsync(text);
        Assert.True(errors.Count == 0, "Ложные срабатывания на корректных формах:\n" + string.Join("\n", errors.Select(e => $"  • {e.Word} @ {e.StartIndex}")));
    }

    [Fact]
    public async Task UrlAndEmail_AreSkipped()
    {
        var checker = CreateSpellchecker();
        const string text = "Перейдите на https://example.com или напишите на test@mail.ru";
        var errors = await checker.CheckTextAsync(text);
        
        // "Перейдите", "на", "или", "напишите", "на" — правильные. 
        // "example" и "mail" не должны проверяться как слова.
        Assert.Empty(errors);
    }

    [Fact]
    public async Task HyphenatedWord_IsCheckedCorrectly()
    {
        var checker = CreateSpellchecker();
        const string text = "по-русски кто-то где-нибудь";
        
        // Эти слова должны быть в словаре, либо не разрываться на части
        var errors = await checker.CheckTextAsync(text);
        
        // Если слово в словаре есть, ошибок нет. 
        // Если нет, мы проверяем, что оно не разорвано на 2 ошибки (по 1 на каждую часть).
        foreach (var err in errors)
        {
            Assert.DoesNotContain("-", err.Word); // Убеждаемся, что дефис внутри слова
            Assert.True(err.Word == "кто-то" || err.Word == "по-русски" || err.Word == "где-нибудь");
        }
    }

    [Fact]
    public async Task LayoutMismatch_ProvidesCorrectSuggestion()
    {
        var checker = CreateSpellchecker();
        // ghbdtn на английской раскладке = привет
        const string text = "ghbdtn"; 
        
        var errors = await checker.CheckTextAsync(text);
        
        var error = Assert.Single(errors);
        Assert.Equal("ghbdtn", error.Word);
        // Проверяем, что первое предложение — это исправленная раскладка
        Assert.Equal("привет", error.Suggestions.First());
    }

    [Fact]
    public async Task UnknownWord_ProducesErrorWithCorrectPosition()
    {
        var checker = CreateSpellchecker();
        const string unknown = "фывафыва";
        var text = $"привет {unknown} мир";
        var errors = await checker.CheckTextAsync(text);

        var error = Assert.Single(errors);
        Assert.Equal(unknown, error.Word);
        Assert.Equal(text.IndexOf(unknown, StringComparison.Ordinal), error.StartIndex);
        Assert.Equal(unknown.Length, error.Length);
        Assert.Equal(ErrorType.Spelling, error.ErrorType);
        Assert.NotEmpty(error.Suggestions);
    }

    [Fact]
    public async Task UserDictionaryWord_IsNotFlagged()
    {
        const string customWord = "квазинеологизм";
        var dictionary = new StubDictionary(customWord);
        var checker = CreateSpellchecker(dictionary);

        var errors = await checker.CheckTextAsync(customWord);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task MultipleErrors_AreOrderedByStartIndex()
    {
        var checker = CreateSpellchecker();
        const string text = "фывафыва дом йцукйцук";
        var errors = await checker.CheckTextAsync(text);

        Assert.Equal(2, errors.Count);
        Assert.True(errors[0].StartIndex < errors[1].StartIndex);
    }

    [Fact]
    public async Task Cancellation_IsRespected()
    {
        var checker = CreateSpellchecker();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => checker.CheckTextAsync("мама папа дом", cts.Token));
    }

    private sealed class EmptyDictionary : IDictionaryService
    {
        public bool IsWordKnown(string word) => false;
        public Task AddWordAsync(string word) => Task.CompletedTask;
    }

    private sealed class StubDictionary : IDictionaryService
    {
        private readonly HashSet<string> _words;
        public StubDictionary(params string[] words) => _words = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
        public bool IsWordKnown(string word) => _words.Contains(word);
        public Task AddWordAsync(string word) { _words.Add(word); return Task.CompletedTask; }
    }
}