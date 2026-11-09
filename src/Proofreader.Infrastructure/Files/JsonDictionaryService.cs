using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;

namespace Proofreader.Infrastructure.Files;

public class JsonDictionaryService : IDictionaryService
{
    private readonly HashSet<string> _words = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _filePath;
    private readonly object _lock = new();

    public JsonDictionaryService()
    {
        // Файл будет храниться в AppData/Local/Proofreader/custom_dictionary.json
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appDataPath, "Proofreader");
        
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "custom_dictionary.json");
        LoadDictionary();
    }

    private void LoadDictionary()
    {
        if (!File.Exists(_filePath)) return;

        try
        {
            var json = File.ReadAllText(_filePath);
            var words = JsonSerializer.Deserialize<List<string>>(json);
            if (words != null)
            {
                lock (_lock)
                {
                    foreach (var word in words)
                    {
                        _words.Add(word);
                    }
                }
            }
        }
        catch
        {
            // Если файл поврежден, просто игнорируем его
        }
    }

    public bool IsWordKnown(string word)
    {
        lock (_lock)
        {
            return _words.Contains(word);
        }
    }

    public async Task AddWordAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        lock (_lock)
        {
            if (!_words.Add(word)) return; // Если слово уже есть, ничего не делаем
        }

        // Сохраняем обновленный список на диск
        var wordsList = new List<string>(_words);
        var json = JsonSerializer.Serialize(wordsList, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
}