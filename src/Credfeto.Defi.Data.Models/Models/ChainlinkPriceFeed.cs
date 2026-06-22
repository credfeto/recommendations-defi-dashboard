using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Symbol} price={CurrentPrice}")]
public readonly record struct ChainlinkPriceFeed(string Symbol, decimal CurrentPrice);
