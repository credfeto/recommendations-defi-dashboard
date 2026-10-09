using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama.LoggingExtensions;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.DefiLlama;

public sealed class DefiLlamaProtocolsClient : IDefiLlamaProtocolsClient
{
    private const string PROTOCOLS_URL = "https://api.llama.fi/protocols";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefiLlamaProtocolsClient> _logger;

    public DefiLlamaProtocolsClient(IHttpClientFactory httpClientFactory, ILogger<DefiLlamaProtocolsClient> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

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
