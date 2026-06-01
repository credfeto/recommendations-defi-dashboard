using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.Server.ApiClients.GoPlus.LoggingExtensions;

internal static partial class GoPlusClientLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to fetch token security from GoPlus for chain {Chain}"
    )]
    public static partial void FetchTokenSecurityFailed(this ILogger logger, string chain, Exception exception);
}
