using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;
using WeCantSpell.Hunspell;

namespace Proofreader.Language;

public class HunspellSpellchecker : ISpellchecker
{
    private readonly WordList _wordList;

    public HunspellSpellchecker()
    {
        // Определяем путь к словарю (файл .dic)
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dicPath = Path.Combine(baseDir, "Dictionaries", "ru_RU.dic");

        if (!File.Exists(dicPath))
            throw new FileNotFoundException($"Словарь не найден по пути: {dicPath}");

        // Загружаем словарь в память
        _wordList = WordList.CreateFromFiles(dicPath);
    }

    public Task<IReadOnlyList<SpellingError>> CheckTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<SpellingError>>(() =>
        {
            var errors = new List<SpellingError>();
            
            // Разбиваем текст на слова с помощью регулярного выражения.
            // Ищем только кириллические слова.
            var wordRegex = new Regex(@"[а-яА-ЯёЁ]+", RegexOptions.Compiled);
            
            // Проверяем, не отменена ли операция
            cancellationToken.ThrowIfCancellationRequested();

            var matches = wordRegex.Matches(text);
            foreach (Match match in matches)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var word = match.Value;
                
                // Проверяем слово (игнорируем регистр)
                if (!_wordList.Check(word))
                {
                    // Если слова нет в словаре, получаем варианты исправлений (максимум 5)
                    var suggestions = _wordList.Suggest(word).Take(5).ToList();
                    
                    errors.Add(new SpellingError
                    {
                        Word = word,
                        StartIndex = match.Index,
                        Length = match.Length,
                        Suggestions = suggestions
                    });
                }
            }

            return errors;
        }, cancellationToken);
    }
}