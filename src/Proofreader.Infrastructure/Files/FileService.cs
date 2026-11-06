using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;

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
                return await Task.Run(() => ExtractTextFromDocx(filePath));
            else
                return await File.ReadAllTextAsync(filePath);
        }
        catch { return null; }
    }

    public async Task SaveTextAsync(string filePath, string text, string? originalFilePath, IEnumerable<SpellingError> fixedErrors)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".docx")
        {
            if (!string.IsNullOrEmpty(originalFilePath) && File.Exists(originalFilePath) && Path.GetExtension(originalFilePath).ToLowerInvariant() == ".docx")
            {
                // Если мы сохраняем на основе существующего DOCX — копируем его и меняем текст
                await Task.Run(() => 
                {
                    // Копируем оригинал в новый файл
                    File.Copy(originalFilePath, filePath, true);
                    
                    // Открываем скопированный файл для редактирования
                    using var wordDoc = WordprocessingDocument.Open(filePath, true);
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body == null) return;

                    // Применяем замены точечно
                    foreach (var error in fixedErrors)
                    {
                        if (string.IsNullOrEmpty(error.AppliedSuggestion)) continue;

                        // Ищем все текстовые узлы (Text)
                        var textNodes = body.Descendants<Text>().ToList();
                        foreach (var textNode in textNodes)
                        {
                            // Проверяем, содержится ли ошибочное слово в этом узле
                            // (Мы используем простое совпадение, в будущем можно сделать умнее)
                            if (textNode.Text.Contains(error.Word))
                            {
                                // Заменяем, сохраняя форматирование (стили узла Text не меняются)
                                textNode.Text = textNode.Text.Replace(error.Word, error.AppliedSuggestion);
                                // Нам не нужно менять свойство Space, так как мы меняем только середину строки
                            }
                        }
                    }
                    
                    wordDoc.MainDocumentPart!.Document!.Save();
                });
            }
            else
            {
                // Если оригинал не DOCX, создаем новый, как раньше (просто текст)
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
            var mainPart = wordDoc.MainDocumentPart;
            if (mainPart?.Document?.Body == null) return string.Empty;

            var paragraphs = mainPart.Document.Body.Descendants<Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                sb.AppendLine(paragraph.InnerText);
            }
        }
        return sb.ToString();
    }
}