using System.Collections.Generic;
using System.Threading.Tasks;
using Proofreader.Core.Models;

namespace Proofreader.Core.Interfaces;

public interface IFileService
{
    Task<string?> LoadTextAsync(string filePath);
    Task SaveTextAsync(string filePath, string text, string? originalFilePath, IEnumerable<SpellingError> fixedErrors);
}