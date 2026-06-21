using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.Chainlink.Interfaces;

/// <summary>
///     Fetches stablecoin USD prices from Chainlink on-chain AggregatorV3Interface price feeds.
/// </summary>
public interface IChainlinkStablecoinsClient
{
    /// <summary>
    ///     Fetches current USD prices for known stablecoins from Chainlink on-chain price feeds.
    ///     Returns an empty list if no Ethereum RPC URL is configured.
    /// </summary>
    ValueTask<IReadOnlyList<ChainlinkPriceFeed>> FetchStablecoinsAsync(CancellationToken cancellationToken);
}
