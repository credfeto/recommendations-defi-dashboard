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

public sealed class DefiLlamaStablecoinsClient : IDefiLlamaStablecoinsClient
{
    private const string LLAMA_STABLECOINS_URL = "https://stablecoins.llama.fi/stablecoins";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefiLlamaStablecoinsClient> _logger;

    public DefiLlamaStablecoinsClient(IHttpClientFactory httpClientFactory, ILogger<DefiLlamaStablecoinsClient> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    public async ValueTask<IReadOnlyList<DefiLlamaStablecoin>> FetchStablecoinsAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            using HttpClient client = this._httpClientFactory.CreateClient(nameof(DefiLlamaStablecoinsClient));
            DefiLlamaStablecoinsResponse? response = await client.GetFromJsonAsync(
                requestUri: LLAMA_STABLECOINS_URL,
                jsonTypeInfo: AppJsonContext.Default.DefiLlamaStablecoinsResponse,
                cancellationToken: cancellationToken
            );

            return response?.PeggedAssets ?? [];
        }
        catch (Exception ex)
        {
            this._logger.FetchStablecoinsFailed(ex);

            return [];
        }
    }
}
