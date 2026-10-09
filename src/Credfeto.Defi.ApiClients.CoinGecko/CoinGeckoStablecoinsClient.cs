using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko.LoggingExtensions;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.CoinGecko;

public sealed class CoinGeckoStablecoinsClient : ICoinGeckoStablecoinsClient
{
    private const string BASE_URL = "https://api.coingecko.com/api/v3";
    private const int PER_PAGE = 250;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CoinGeckoStablecoinsClient> _logger;

    public CoinGeckoStablecoinsClient(IHttpClientFactory httpClientFactory, ILogger<CoinGeckoStablecoinsClient> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    public async ValueTask<IReadOnlyList<CoinGeckoStablecoin>> FetchStablecoinsAsync(
        CancellationToken cancellationToken
    )
    {
        List<CoinGeckoStablecoin> all = [];

        try
        {
            using HttpClient client = this._httpClientFactory.CreateClient(nameof(CoinGeckoStablecoinsClient));
            int page = 1;

            while (true)
            {
                string url = string.Format(
                    provider: CultureInfo.InvariantCulture,
                    format: "{0}/coins/markets?vs_currency=usd&category=stablecoins&order=market_cap_desc&per_page={1}&page={2}",
                    BASE_URL,
                    PER_PAGE,
                    page
                );

                CoinGeckoStablecoin[]? results = await client.GetFromJsonAsync(
                    requestUri: url,
                    jsonTypeInfo: AppJsonContext.Default.CoinGeckoStablecoinArray,
                    cancellationToken: cancellationToken
                );

                if (results is null || results.Length == 0)
                {
                    break;
                }

                all.AddRange(results);

                if (results.Length < PER_PAGE)
                {
                    break;
                }

                page++;
            }
        }
        catch (Exception ex)
        {
            this._logger.FetchStablecoinsFailed(ex);
        }

        return all;
    }

    public async ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>> FetchCoinListAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            using HttpClient client = this._httpClientFactory.CreateClient(nameof(CoinGeckoStablecoinsClient));
            string url = $"{BASE_URL}/coins/list?include_platform=true";
            CoinGeckoCoinPlatforms[]? result = await client.GetFromJsonAsync(
                requestUri: url,
                jsonTypeInfo: AppJsonContext.Default.CoinGeckoCoinPlatformsArray,
                cancellationToken: cancellationToken
            );

            return result ?? [];
        }
        catch (Exception ex)
        {
            this._logger.FetchCoinListFailed(ex);

            return [];
        }
    }
}
