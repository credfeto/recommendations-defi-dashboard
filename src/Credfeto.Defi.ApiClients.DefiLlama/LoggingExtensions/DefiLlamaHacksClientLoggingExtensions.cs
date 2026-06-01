using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.DefiLlama.LoggingExtensions;

internal static partial class DefiLlamaHacksClientLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to fetch hacks from DefiLlama")]
    public static partial void FetchHacksFailed(this ILogger logger, Exception exception);
}
