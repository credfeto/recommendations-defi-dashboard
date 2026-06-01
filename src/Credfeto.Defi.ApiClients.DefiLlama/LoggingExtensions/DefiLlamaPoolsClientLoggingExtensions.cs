using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.DefiLlama.LoggingExtensions;

internal static partial class DefiLlamaPoolsClientLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to fetch pools from DefiLlama")]
    public static partial void FetchPoolsFailed(this ILogger logger, Exception exception);
}
