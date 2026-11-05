using System.Threading.Tasks;

namespace Proofreader.Core.Interfaces;

public interface IFileService
{
    Task<string?> LoadTextAsync(string filePath);
    Task SaveTextAsync(string filePath, string text);
}