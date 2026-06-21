using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Stablecoin price entry from a Chainlink on-chain AggregatorV3Interface price feed.
/// </summary>
[DebuggerDisplay("{Symbol} price={CurrentPrice}")]
public sealed record ChainlinkPriceFeed
{
    /// <summary>
    ///     Stablecoin symbol in lowercase (e.g. "usdc").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    ///     Current price in USD.
    /// </summary>
    public required decimal CurrentPrice { get; init; }
}
