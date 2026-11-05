using System.Threading.Tasks;

namespace Proofreader.Core.Interfaces;

public interface ITextFileService
{
    Task<string?> LoadTextAsync(string filePath);
    Task SaveTextAsync(string filePath, string text);
}