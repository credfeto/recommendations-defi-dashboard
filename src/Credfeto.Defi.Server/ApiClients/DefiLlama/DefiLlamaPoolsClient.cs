using System;
using System.Collections.Generic;
using System.Linq;
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
///     Fetches yield pool data from the DefiLlama API.
/// </summary>
public sealed class DefiLlamaPoolsClient
{
    private const string LLAMA_POOLS_URL = "https://yields.llama.fi/pools";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefiLlamaPoolsClient> _logger;

    /// <summary>
    ///     Initialises a new instance of <see cref="DefiLlamaPoolsClient" />.
    /// </summary>
    public DefiLlamaPoolsClient(IHttpClientFactory httpClientFactory, ILogger<DefiLlamaPoolsClient> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <summary>
    ///     Fetches all yield pools from DefiLlama.
    ///     Pendle pools are excluded — the Pendle API is the authoritative source for those.
    /// </summary>
    public async ValueTask<IReadOnlyList<RawPool>> FetchPoolsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = this._httpClientFactory.CreateClient(nameof(DefiLlamaPoolsClient));
            DefiLlamaPoolsResponse? response = await client.GetFromJsonAsync(
                requestUri: LLAMA_POOLS_URL,
                jsonTypeInfo: AppJsonContext.Default.DefiLlamaPoolsResponse,
                cancellationToken: cancellationToken
            );

            if (response?.Data is null)
            {
                return [];
            }

            return
            [
                .. response.Data.Where(pool =>
                    !string.Equals(a: pool.Project, b: "pendle", comparisonType: StringComparison.OrdinalIgnoreCase)
                ),
            ];
        }
        catch (Exception ex)
        {
            this._logger.FetchPoolsFailed(ex);

            return [];
        }
    }
}
