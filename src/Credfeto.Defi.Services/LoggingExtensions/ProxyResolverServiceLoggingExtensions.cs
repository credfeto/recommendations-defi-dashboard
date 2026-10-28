using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.Services.LoggingExtensions;

internal static partial class ProxyResolverServiceLoggingExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Failed to call eth_getStorageAt for address {Address} slot {Slot}"
    )]
    public static partial void GetStorageAtFailed(
        this ILogger logger,
        string address,
        string slot,
        Exception exception
    );

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Failed to call implementation() on beacon {Beacon}")]
    public static partial void BeaconImplementationCallFailed(this ILogger logger, string beacon, Exception exception);
}
