using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.CoinGecko.Interfaces;

/// <summary>
///     Fetches stablecoin price data and the full coin list from CoinGecko.
/// </summary>
public interface ICoinGeckoStablecoinsClient
{
    /// <summary>
    ///     Fetches all stablecoins by paginating through the CoinGecko markets endpoint.
    /// </summary>
    ValueTask<IReadOnlyList<CoinGeckoStablecoin>> FetchStablecoinsAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Fetches the full CoinGecko coin list with on-chain contract addresses.
    /// </summary>
    ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>> FetchCoinListAsync(CancellationToken cancellationToken);
}
