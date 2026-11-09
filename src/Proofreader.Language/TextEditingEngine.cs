using System.Threading;
using System.Threading.Tasks;

namespace Proofreader.Language;

public class TextEditingEngine
{
    private static LocalLanguageModelService? _sharedLlm;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public static async Task<TextEditingEngine> CreateAsync(string modelPath)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_sharedLlm == null)
            {
                _sharedLlm = await LocalLanguageModelService.CreateAsync(modelPath);
            }
            return new TextEditingEngine(_sharedLlm);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private readonly LocalLanguageModelService _llm;

    private TextEditingEngine(LocalLanguageModelService llm)
    {
        _llm = llm;
    }

    public async Task<string> FixErrorsAsync(string text, CancellationToken cancellationToken = default)
    {
        var systemPrompt = "Ты — корректор. Найди в тексте орфографические, пунктуационные и грамматические ошибки и исправь их. " +
                           "Не меняй стиль автора, не переписывай предложения, если в них нет ошибок. " +
                           "Выведи ТОЛЬКО исправленный текст без лишних слов.";

        return await _llm.GenerateAsync(systemPrompt, text, cancellationToken);
    }
}