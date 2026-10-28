using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.DefiLlama.LoggingExtensions;

internal static partial class DefiLlamaStablecoinsClientLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to fetch stablecoins from DefiLlama")]
    public static partial void FetchStablecoinsFailed(this ILogger logger, Exception exception);
}
