using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Models;

namespace Proofreader.Core.Interfaces;

public interface IProofreaderEngine
{
    Task<IReadOnlyList<SpellingError>> CheckDocumentAsync(
        string text,
        CancellationToken cancellationToken = default);
}