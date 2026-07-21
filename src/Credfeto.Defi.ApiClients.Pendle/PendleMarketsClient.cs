using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Pendle.Interfaces;
using Credfeto.Defi.ApiClients.Pendle.LoggingExtensions;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.Pendle;

/// <summary>
///     Fetches raw Pendle market data.
/// </summary>
public sealed class PendleMarketsClient : IPendleMarketsClient
{
    private const string PENDLE_API_BASE = "https://api-v2.pendle.finance/core/v1";
    private const int PAGE_LIMIT = 100;

    private static readonly int[] PendleChainIds = [1, 42161, 8453, 56];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PendleMarketsClient> _logger;

    /// <summary>
    ///     Initialises a new instance of <see cref="PendleMarketsClient" />.
    /// </summary>
    public PendleMarketsClient(IHttpClientFactory httpClientFactory, ILogger<PendleMarketsClient> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <summary>
    ///     Fetches all markets from Pendle across all supported chains.
    /// </summary>
    public async ValueTask<IReadOnlyList<PendleMarket>> FetchMarketsAsync(CancellationToken cancellationToken)
    {
        List<PendleMarket> all = [];

        foreach (int chainId in PendleChainIds)
        {
            try
            {
                IReadOnlyList<PendleMarket> markets = await this.FetchMarketsForChainAsync(
                    chainId: chainId,
                    cancellationToken: cancellationToken
                );

                all.AddRange(markets);
            }
            catch (Exception ex)
            {
                this._logger.FetchChainMarketsFailed(chainId: chainId, exception: ex);
            }
        }

        return all;
    }

    private async ValueTask<IReadOnlyList<PendleMarket>> FetchMarketsForChainAsync(
        int chainId,
        CancellationToken cancellationToken
    )
    {
        List<PendleMarket> markets = [];
        int skip = 0;
        int total = int.MaxValue;

        using HttpClient client = this._httpClientFactory.CreateClient(nameof(PendleMarketsClient));

        while (skip < total)
        {
            string url = string.Format(
                provider: CultureInfo.InvariantCulture,
                format: "{0}/{1}/markets?limit={2}&skip={3}&select=all",
                PENDLE_API_BASE,
                chainId,
                PAGE_LIMIT,
                skip
            );

            PendleMarketsResponse? response = await client.GetFromJsonAsync(
                requestUri: url,
                jsonTypeInfo: AppJsonContext.Default.PendleMarketsResponse,
                cancellationToken: cancellationToken
            );

            if (response?.Results is null || response.Results.Length == 0)
            {
                break;
            }

            markets.AddRange(response.Results);
            total = response.Total;
            skip += response.Results.Length;
        }

        return markets;
    }
}
