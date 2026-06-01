using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.Pendle.LoggingExtensions;
using Credfeto.Defi.Server.Json;
using Credfeto.Defi.Server.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.Server.ApiClients.Pendle;

/// <summary>
///     Fetches Pendle market data and normalises it into the common pool format.
/// </summary>
public sealed class PendleMarketsClient
{
    private const string PENDLE_API_BASE = "https://api-v2.pendle.finance/core/v1";
    private const int PAGE_LIMIT = 100;

    private static readonly int[] PendleChainIds = [1, 42161, 8453, 56];

    private static readonly IReadOnlyDictionary<int, string> ChainIdToName = new Dictionary<int, string>
    {
        [1] = "Ethereum",
        [42161] = "Arbitrum",
        [8453] = "Base",
        [56] = "BSC",
    };

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
    ///     Fetches all active markets from Pendle across all supported chains
    ///     and normalises them into the common pool format.
    /// </summary>
    public async ValueTask<IReadOnlyList<RawPool>> FetchMarketsAsync(CancellationToken cancellationToken)
    {
        List<RawPool> all = [];

        foreach (int chainId in PendleChainIds)
        {
            try
            {
                IReadOnlyList<PendleMarket> markets = await this.FetchMarketsForChainAsync(
                    chainId: chainId,
                    cancellationToken: cancellationToken
                );

                foreach (PendleMarket market in markets)
                {
                    if (market.IsActive)
                    {
                        all.Add(NormaliseMarket(market));
                    }
                }
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

    private static RawPool NormaliseMarket(PendleMarket market)
    {
        string chain = ChainIdToName.TryGetValue(key: market.ChainId, out string? name)
            ? name
            : market.ChainId.ToString(CultureInfo.InvariantCulture);

        string? poolMeta = null;

        if (!string.IsNullOrEmpty(market.Expiry))
        {
            if (
                DateTimeOffset.TryParse(
                    input: market.Expiry,
                    formatProvider: CultureInfo.InvariantCulture,
                    styles: System.Globalization.DateTimeStyles.None,
                    result: out DateTimeOffset expiry
                )
            )
            {
                poolMeta =
                    $"Maturity {expiry.ToString(format: "dd MMM yyyy", formatProvider: CultureInfo.InvariantCulture)}";
            }
        }

        bool isStable =
            market.CategoryIds is not null && market.CategoryIds.Contains("stables", StringComparer.OrdinalIgnoreCase);

        return new RawPool
        {
            Chain = chain,
            Project = "pendle",
            Symbol = market.SimpleSymbol,
            TvlUsd = market.Liquidity?.Usd ?? 0,
            Apy = market.AggregatedApy * 100,
            ApyBase = market.UnderlyingApy * 100,
            ApyReward = (market.PendleApy + market.LpRewardApy + market.SwapFeeApy) * 100,
            IlRisk = "no",
            Stablecoin = isStable,
            PoolId = market.Address,
            PoolMeta = poolMeta,
            VolumeUsd1d = market.TradingVolume?.Usd,
            // DataSource is used in URL generation; we embed it via a convention
            // and handle it specially in the enrichment service.
            Predictions = new RawPredictions(),
            Outlier = false,
        };
    }
}
