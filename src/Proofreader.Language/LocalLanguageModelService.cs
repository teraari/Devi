using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging.Abstractions;

namespace Proofreader.Language;

public class LocalLanguageModelService : IDisposable
{
    private readonly LLamaWeights _weights;
    private readonly ModelParams _modelParams;

    private LocalLanguageModelService(LLamaWeights weights, ModelParams modelParams)
    {
        _weights = weights;
        _modelParams = modelParams;
    }

    public static async Task<LocalLanguageModelService> CreateAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Файл модели не найден: {modelPath}");

        return await Task.Run(() =>
        {
            var modelParams = new ModelParams(modelPath)
            {
                ContextSize = 2048,
                GpuLayerCount = 20
            };
            var weights = LLamaWeights.LoadFromFile(modelParams);
            return new LocalLanguageModelService(weights, modelParams);
        });
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userText, CancellationToken cancellationToken = default)
    {
        var prompt = $@"<|start_header_id|>system<|end_header_id|>
{systemPrompt}<|eot_id|><|start_header_id|>user<|end_header_id|>
{userText}<|eot_id|><|start_header_id|>assistant<|end_header_id|>
";

        var executor = new StatelessExecutor(_weights, _modelParams, NullLogger.Instance);

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 1024,
            AntiPrompts = new List<string> { "<|eot_id|>" },
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.2f }
        };

        var sb = new StringBuilder();
        await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            sb.Append(token);
        }

        return sb.ToString().Trim();
    }

    public void Dispose()
    {
        _weights?.Dispose();
    }
}