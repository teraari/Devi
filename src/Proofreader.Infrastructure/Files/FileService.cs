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
                // По умолчанию читаем как обычный текст (для .txt)
                return await File.ReadAllTextAsync(filePath);
            }
        }
        catch (Exception)
        {
            // Возвращаем null, если не удалось прочитать файл (например, он поврежден)
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
                // Создаем НОВЫЙ документ Word
                using var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Разбиваем текст на строки, чтобы сделать абзацы
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
            // Обычное сохранение текста для .txt и других форматов
            await File.WriteAllTextAsync(filePath, text);
        }
    }

    // Метод для извлечения текста из DOCX
    private string ExtractTextFromDocx(string filePath)
    {
        var sb = new StringBuilder();

        // Открываем документ ТОЛЬКО ДЛЯ ЧТЕНИЯ (false)
        using (var wordDoc = WordprocessingDocument.Open(filePath, false))
        {
            if (wordDoc.MainDocumentPart?.Document.Body == null) return string.Empty;

            // Извлекаем все абзацы
            var paragraphs = wordDoc.MainDocumentPart.Document.Body.Descendants<Paragraph>();
            
            foreach (var paragraph in paragraphs)
            {
                // Берем текст абзаца и добавляем перенос строки
                sb.AppendLine(paragraph.InnerText);
            }
        }

        return sb.ToString();
    }
}