using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Address}/{ChainId} = {CategoryId}")]
public sealed record PendleMarketCategoryRow(
    string Address,
    int ChainId,
    string CategoryId,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
