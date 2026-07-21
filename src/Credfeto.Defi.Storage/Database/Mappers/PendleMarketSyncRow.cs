using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Address} ({SimpleSymbol}) ChainId={ChainId}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in PendleMarketStorageService"
)]
internal sealed record PendleMarketSyncRow(
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
    double? TradingVolumeUsd
);
