using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.Pendle.Interfaces;

/// <summary>
///     Fetches raw Pendle market data.
/// </summary>
public interface IPendleMarketsClient
{
    /// <summary>
    ///     Fetches all markets from Pendle across all supported chains.
    /// </summary>
    ValueTask<IReadOnlyList<PendleMarket>> FetchMarketsAsync(CancellationToken cancellationToken);
}
