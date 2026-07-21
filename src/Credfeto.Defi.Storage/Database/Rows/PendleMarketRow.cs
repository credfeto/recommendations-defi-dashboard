using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Address} ({SimpleSymbol}) ChainId={ChainId}")]
public sealed record PendleMarketRow(
    string Address,
    int ChainId,
    string SimpleSymbol,
    string? Expiry,
    bool IsActive,
    double? LiquidityUsd,
    double AggregatedApy,
    double UnderlyingApy,
    double PendleApy,
    double LpRewardApy,
    double SwapFeeApy,
    double? TradingVolumeUsd,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
