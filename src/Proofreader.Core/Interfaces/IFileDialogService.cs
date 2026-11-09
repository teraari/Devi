using System.Threading.Tasks;

namespace Proofreader.Core.Interfaces;

public interface IFileDialogService
{
    Task<string?> PickTextFileToOpenAsync();

    Task<string?> PickTextFileToSaveAsync(string suggestedName);
}