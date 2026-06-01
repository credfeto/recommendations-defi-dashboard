using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.Pendle.Interfaces;

/// <summary>
///     Fetches Pendle market data and normalises it into the common pool format.
/// </summary>
public interface IPendleMarketsClient
{
    /// <summary>
    ///     Fetches all active markets from Pendle across all supported chains.
    /// </summary>
    ValueTask<IReadOnlyList<RawPool>> FetchMarketsAsync(CancellationToken cancellationToken);
}
