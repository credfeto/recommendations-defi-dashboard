using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.Services.LoggingExtensions;

internal static partial class CacheWarmerServiceLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Cache warmed: {Key}")]
    public static partial void CacheWarmed(this ILogger logger, string key);
}
