using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.HoneypotIs.LoggingExtensions;

internal static partial class HoneypotIsClientLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to fetch token security from Honeypot.is for chain {Chain} address {Address}"
    )]
    public static partial void FetchTokenSecurityFailed(
        this ILogger logger,
        string chain,
        string address,
        Exception exception
    );
}
