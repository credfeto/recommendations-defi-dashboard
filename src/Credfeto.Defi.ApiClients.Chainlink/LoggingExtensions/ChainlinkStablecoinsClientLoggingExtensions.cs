using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.Chainlink.LoggingExtensions;

internal static partial class ChainlinkStablecoinsClientLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to fetch Chainlink price feed for {symbol} ({feedAddress})")]
    public static partial void FetchFeedFailed(this ILogger logger, string symbol, string feedAddress, Exception exception);
}
