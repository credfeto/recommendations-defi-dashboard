using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.Webacy.LoggingExtensions;

internal static partial class WebacyRiskClientLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to fetch token risk from Webacy for chain {Chain} address {Address}"
    )]
    public static partial void FetchTokenRiskFailed(
        this ILogger logger,
        string chain,
        string address,
        Exception exception
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Webacy returned {StatusCode} for chain {Chain} address {Address}; no risk data recorded"
    )]
    public static partial void FetchTokenRiskUnavailable(
        this ILogger logger,
        string chain,
        string address,
        HttpStatusCode statusCode
    );
}
