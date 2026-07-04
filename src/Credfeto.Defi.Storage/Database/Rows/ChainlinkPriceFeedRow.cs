using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Symbol} price={CurrentPrice}")]
public sealed record ChainlinkPriceFeedRow(
    string Symbol,
    decimal CurrentPrice,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
