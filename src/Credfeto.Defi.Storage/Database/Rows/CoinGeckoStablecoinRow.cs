using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Symbol} price={CurrentPrice}")]
public sealed record CoinGeckoStablecoinRow(
    string Id,
    string Symbol,
    string Name,
    decimal? CurrentPrice,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
