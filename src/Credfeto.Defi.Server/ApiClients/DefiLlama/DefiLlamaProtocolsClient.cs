using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.DefiLlama.LoggingExtensions;
using Credfeto.Defi.Server.Json;
using Credfeto.Defi.Server.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.Server.ApiClients.DefiLlama;

/// <summary>
///     Fetches protocol metadata (including audit information) from the DefiLlama protocols API.
/// </summary>
public sealed class DefiLlamaProtocolsClient
{
    private const string PROTOCOLS_URL = "https://api.llama.fi/protocols";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefiLlamaProtocolsClient> _logger;

    /// <summary>
    ///     Initialises a new instance of <see cref="DefiLlamaProtocolsClient" />.
    /// </summary>
    public DefiLlamaProtocolsClient(IHttpClientFactory httpClientFactory, ILogger<DefiLlamaProtocolsClient> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <summary>
    ///     Fetches all protocol metadata from DefiLlama.
    /// </summary>
    public async ValueTask<IReadOnlyList<RawProtocol>> FetchProtocolsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = this._httpClientFactory.CreateClient(nameof(DefiLlamaProtocolsClient));
            RawProtocol[]? protocols = await client.GetFromJsonAsync(
                requestUri: PROTOCOLS_URL,
                jsonTypeInfo: AppJsonContext.Default.RawProtocolArray,
                cancellationToken: cancellationToken
            );

            return protocols ?? [];
        }
        catch (Exception ex)
        {
            this._logger.FetchProtocolsFailed(ex);

            return [];
        }
    }
}
