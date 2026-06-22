using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{PoolId}[{SortOrder}] = {TokenAddress}")]
public sealed record DefiLlamaPoolRewardTokenRow(
    string PoolId,
    int SortOrder,
    string TokenAddress,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
