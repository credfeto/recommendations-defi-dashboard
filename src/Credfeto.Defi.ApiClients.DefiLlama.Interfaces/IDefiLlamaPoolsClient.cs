using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.DefiLlama.Interfaces;

/// <summary>
///     Fetches yield pool data from the DefiLlama API.
/// </summary>
public interface IDefiLlamaPoolsClient
{
    /// <summary>
    ///     Fetches all yield pools from DefiLlama.
    /// </summary>
    ValueTask<IReadOnlyList<RawPool>> FetchPoolsAsync(CancellationToken cancellationToken);
}
