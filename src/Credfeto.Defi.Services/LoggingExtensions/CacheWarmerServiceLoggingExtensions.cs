using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.Services.LoggingExtensions;

internal static partial class CacheWarmerServiceLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Cache warmed: {Key}")]
    public static partial void CacheWarmed(this ILogger logger, string key);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Cache warm failed: {Key}")]
    public static partial void CacheWarmFailed(this ILogger logger, string key, Exception exception);
}
