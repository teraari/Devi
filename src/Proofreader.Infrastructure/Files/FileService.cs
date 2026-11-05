using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Proofreader.Core.Interfaces;

namespace Proofreader.Infrastructure.Files;

public class FileService : IFileService
{
    public async Task<string?> LoadTextAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            if (extension == ".docx")
            {
                return await Task.Run(() => ExtractTextFromDocx(filePath));
            }
            else
            {
                return await File.ReadAllTextAsync(filePath);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SaveTextAsync(string filePath, string text)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".docx")
        {
            await Task.Run(() => 
            {
                using var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                
                foreach (var line in lines)
                {
                    var paragraph = new Paragraph();
                    var run = new Run();
                    var textElement = new Text(line) { Space = SpaceProcessingModeValues.Preserve };
                    
                    run.AppendChild(textElement);
                    paragraph.AppendChild(run);
                    body.AppendChild(paragraph);
                }
                
                mainPart.Document.Save();
            });
        }
        else
        {
            await File.WriteAllTextAsync(filePath, text);
        }
    }

    private string ExtractTextFromDocx(string filePath)
    {
        var sb = new StringBuilder();

        using (var wordDoc = WordprocessingDocument.Open(filePath, false))
        {
            // ИСПРАВЛЕНИЕ ПРЕДУПРЕЖДЕНИЯ: Проверяем на null пошагово
            var mainPart = wordDoc.MainDocumentPart;
            if (mainPart?.Document?.Body == null) return string.Empty;

            // Теперь компилятор точно знает, что Body не null
            var paragraphs = mainPart.Document.Body.Descendants<Paragraph>();
            
            foreach (var paragraph in paragraphs)
            {
                sb.AppendLine(paragraph.InnerText);
            }
        }

        return sb.ToString();
    }
}