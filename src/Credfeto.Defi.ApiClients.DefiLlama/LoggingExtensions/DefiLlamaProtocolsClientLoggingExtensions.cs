using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.DefiLlama.LoggingExtensions;

internal static partial class DefiLlamaProtocolsClientLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to fetch protocols from DefiLlama")]
    public static partial void FetchProtocolsFailed(this ILogger logger, Exception exception);
}
