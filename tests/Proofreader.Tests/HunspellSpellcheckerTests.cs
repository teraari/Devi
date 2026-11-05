using System.Threading.Tasks;
using Proofreader.Language;
using Xunit;

namespace Proofreader.Tests;

public class HunspellSpellcheckerTests
{
    [Fact]
    public async Task CheckText_ShouldFindSpellingErrors()
    {
        // Arrange
        var spellchecker = new HunspellSpellchecker();
        // "Превет" (Привет) и "сабака" (собака) - классические опечатки
        var text = "Привет мир! Превет, как сабака?"; 

        // Act
        var errors = await spellchecker.CheckTextAsync(text);

        // Assert
        Assert.Equal(2, errors.Count);
        
        // 1. Проверяем "Превет"
        Assert.Equal("Превет", errors[0].Word);
        Assert.Contains("Привет", errors[0].Suggestions); 
        
        // 2. Проверяем "сабака"
        Assert.Equal("сабака", errors[1].Word);
        Assert.Contains("собака", errors[1].Suggestions); 
    }
}