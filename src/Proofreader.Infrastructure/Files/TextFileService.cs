using System;
using System.IO;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;

namespace Proofreader.Infrastructure.Files;

public class TextFileService : ITextFileService
{
    public async Task<string?> LoadTextAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception)
        {
            // В реальном приложении здесь должно быть логирование
            return null;
        }
    }

    public async Task SaveTextAsync(string filePath, string text)
    {
        await File.WriteAllTextAsync(filePath, text);
    }
}