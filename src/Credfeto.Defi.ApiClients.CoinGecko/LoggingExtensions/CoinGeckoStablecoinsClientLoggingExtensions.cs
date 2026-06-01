using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.CoinGecko.LoggingExtensions;

internal static partial class CoinGeckoStablecoinsClientLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to fetch stablecoins from CoinGecko")]
    public static partial void FetchStablecoinsFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to fetch coin list from CoinGecko")]
    public static partial void FetchCoinListFailed(this ILogger logger, Exception exception);
}
