using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proofreader.Core.Models;

namespace Proofreader.Core.Interfaces;

public interface IGrammarChecker
{
    Task<IReadOnlyList<GrammarError>> CheckGrammarAsync(string text, CancellationToken cancellationToken = default);
}