using System.Threading.Tasks;

namespace Proofreader.Core.Interfaces;

public interface IFileDialogService
{
    Task<string?> PickTextFileToOpenAsync();
    
    // Добавлен параметр suggestedName
    Task<string?> PickTextFileToSaveAsync(string suggestedName);
}