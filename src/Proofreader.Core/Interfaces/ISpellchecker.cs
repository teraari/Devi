using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Models;

namespace Proofreader.Core.Interfaces;

public interface ISpellchecker
{
    Task<IReadOnlyList<SpellingError>> CheckTextAsync(string text, CancellationToken cancellationToken = default);
}