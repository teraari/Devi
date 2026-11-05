using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Proofreader.Core.Interfaces;

namespace Proofreader.App.Services;

public class FileDialogService : IFileDialogService
{
    private readonly Window _mainWindow;

    public FileDialogService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public async Task<string?> PickTextFileToOpenAsync()
    {
        var topLevel = TopLevel.GetTopLevel(_mainWindow);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть документ",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("Поддерживаемые файлы") 
                { 
                    Patterns = new[] { "*.txt", "*.docx" } 
                } 
            }
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PickTextFileToSaveAsync(string suggestedName)
    {
        var topLevel = TopLevel.GetTopLevel(_mainWindow);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить документ",
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[] 
            { 
                new FilePickerFileType("Документ Word") { Patterns = new[] { "*.docx" } },
                new FilePickerFileType("Текстовый файл") { Patterns = new[] { "*.txt" } }
            }
        });

        return file?.TryGetLocalPath();
    }
}