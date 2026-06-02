using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.Pendle.LoggingExtensions;

internal static partial class PendleMarketsClientLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to fetch Pendle markets for chain {ChainId}"
    )]
    public static partial void FetchChainMarketsFailed(this ILogger logger, int chainId, Exception exception);
}
