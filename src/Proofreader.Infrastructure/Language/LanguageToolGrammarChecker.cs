using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Interfaces;
using Proofreader.Core.Models;

namespace Proofreader.Infrastructure.Language;

public class LanguageToolGrammarChecker : IGrammarChecker
{
    private readonly HttpClient _httpClient;
    private const string ServerUrl = "http://localhost:8081/v2/check";

    public LanguageToolGrammarChecker()
    {
        _httpClient = new HttpClient();
        // Ставим таймаут, чтобы программа не зависала, если сервер не отвечает
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<IReadOnlyList<GrammarError>> CheckGrammarAsync(string text, CancellationToken cancellationToken = default)
    {
        var errors = new List<GrammarError>();

        var requestData = new Dictionary<string, string>
        {
            { "text", text },
            { "language", "ru-RU" }
        };

        var content = new FormUrlEncodedContent(requestData);

        // Если сервер не запущен, этот запрос выбросит исключение HttpRequestException,
        // которое мы поймаем в MainViewModel и покажем в статус-баре.
        var response = await _httpClient.PostAsync(ServerUrl, content, cancellationToken);
        
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        
        using var document = JsonDocument.Parse(jsonResponse);
        var matches = document.RootElement.GetProperty("matches");

        foreach (var match in matches.EnumerateArray())
        {
            var error = new GrammarError
            {
                Message = match.GetProperty("message").GetString() ?? string.Empty,
                ShortMessage = match.GetProperty("shortMessage").GetString() ?? string.Empty,
                StartIndex = match.GetProperty("offset").GetInt32(),
                Length = match.GetProperty("length").GetInt32()
            };

            // Для пропущенной запятой Length может быть 0, поэтому OriginalText может быть пустым
            if (error.Length > 0 && text.Length >= error.StartIndex + error.Length)
            {
                error.OriginalText = text.Substring(error.StartIndex, error.Length);
            }
            else
            {
                error.OriginalText = string.Empty;
            }

            if (match.TryGetProperty("replacements", out var replacements))
            {
                foreach (var replacement in replacements.EnumerateArray())
                {
                    var value = replacement.GetProperty("value").GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        error.Suggestions.Add(value);
                    }
                }
            }

            errors.Add(error);
        }

        return errors;
    }
}