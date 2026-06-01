using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.CoinGecko;
using Credfeto.Defi.Server.ApiClients.DefiLlama;
using Credfeto.Defi.Server.ApiClients.Pendle;
using Credfeto.Defi.Server.Cache;
using Credfeto.Defi.Server.Json;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Utils;

namespace Credfeto.Defi.Server.Services;

/// <summary>
///     Orchestrates fetching, caching, and enriching pool data from all sources.
/// </summary>
internal sealed class PoolEnrichmentService
{
    private const string CACHE_KEY_LLAMA_POOLS = "defillama_pools";
    private const string CACHE_KEY_PENDLE_POOLS = "pendle_pools";
    private const string CACHE_KEY_HACKS = "defillama_hacks";
    private const string CACHE_KEY_PROTOCOLS = "defillama_protocols";
    private const string CACHE_KEY_STABLECOINS = "coingecko_stablecoins";
    private const string CACHE_KEY_COIN_LIST = "coingecko_coin_list";

    private readonly ApiCacheService _cache;
    private readonly CoinGeckoStablecoinsClient _coinGeckoClient;
    private readonly ContractSecurityService _contractSecurityService;
    private readonly DefiLlamaHacksClient _hacksClient;
    private readonly DefiLlamaPoolsClient _llamaPoolsClient;
    private readonly PendleMarketsClient _pendleClient;
    private readonly DefiLlamaProtocolsClient _protocolsClient;

    /// <summary>
    ///     Initialises a new instance of <see cref="PoolEnrichmentService" />.
    /// </summary>
    public PoolEnrichmentService(
        DefiLlamaPoolsClient llamaPoolsClient,
        PendleMarketsClient pendleClient,
        DefiLlamaHacksClient hacksClient,
        DefiLlamaProtocolsClient protocolsClient,
        CoinGeckoStablecoinsClient coinGeckoClient,
        ContractSecurityService contractSecurityService,
        ApiCacheService cache
    )
    {
        this._llamaPoolsClient = llamaPoolsClient;
        this._pendleClient = pendleClient;
        this._hacksClient = hacksClient;
        this._protocolsClient = protocolsClient;
        this._coinGeckoClient = coinGeckoClient;
        this._contractSecurityService = contractSecurityService;
        this._cache = cache;
    }

    /// <summary>
    ///     Returns all raw pools (DefiLlama + Pendle), using the cache where available.
    /// </summary>
    public async ValueTask<IReadOnlyList<RawPool>> GetAllPoolsAsync(CancellationToken cancellationToken)
    {
        ValueTask<IReadOnlyList<RawPool>> llamaTask = this._cache.GetOrFetchAsync(
            key: CACHE_KEY_LLAMA_POOLS,
            fetcher: this._llamaPoolsClient.FetchPoolsAsync,
            typeInfo: AppJsonContext.Default.IReadOnlyListRawPool,
            cancellationToken: cancellationToken
        );

        ValueTask<IReadOnlyList<RawPool>> pendleTask = this._cache.GetOrFetchAsync(
            key: CACHE_KEY_PENDLE_POOLS,
            fetcher: this._pendleClient.FetchMarketsAsync,
            typeInfo: AppJsonContext.Default.IReadOnlyListRawPool,
            cancellationToken: cancellationToken
        );

        IReadOnlyList<RawPool> llamaPools = await llamaTask;
        IReadOnlyList<RawPool> pendlePools = await pendleTask;

        List<RawPool> all = [.. llamaPools, .. pendlePools];

        return all;
    }

    /// <summary>
    ///     Returns a slug-keyed hack map from the cache or live fetch.
    /// </summary>
    public async ValueTask<IReadOnlyDictionary<string, List<HackInfo>>> GetHackMapAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<RawHack> hacks = await this._cache.GetOrFetchAsync(
            key: CACHE_KEY_HACKS,
            fetcher: this._hacksClient.FetchHacksAsync,
            typeInfo: AppJsonContext.Default.IReadOnlyListRawHack,
            cancellationToken: cancellationToken
        );

        return HacksService.BuildHackMap(hacks);
    }

    /// <summary>
    ///     Returns a slug-keyed protocol audit map from the cache or live fetch.
    /// </summary>
    public async ValueTask<IReadOnlyDictionary<string, AuditInfo>> GetProtocolAuditMapAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<RawProtocol> protocols = await this._cache.GetOrFetchAsync(
            key: CACHE_KEY_PROTOCOLS,
            fetcher: this._protocolsClient.FetchProtocolsAsync,
            typeInfo: AppJsonContext.Default.IReadOnlyListRawProtocol,
            cancellationToken: cancellationToken
        );

        return ProtocolsService.BuildProtocolAuditMap(protocols);
    }

    /// <summary>
    ///     Returns a symbol → price map for stablecoins from the cache or live fetch.
    /// </summary>
    public async ValueTask<IReadOnlyDictionary<string, decimal>> GetStablecoinPriceMapAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<CoinGeckoStablecoin> coins = await this._cache.GetOrFetchAsync(
            key: CACHE_KEY_STABLECOINS,
            fetcher: this._coinGeckoClient.FetchStablecoinsAsync,
            typeInfo: AppJsonContext.Default.IReadOnlyListCoinGeckoStablecoin,
            cancellationToken: cancellationToken
        );

        return DepegService.BuildStablecoinPriceMap(coins);
    }

    /// <summary>
    ///     Returns an address → symbol map for stablecoins from the cache or live fetch.
    /// </summary>
    public async ValueTask<IReadOnlyDictionary<string, string>> GetStablecoinAddressMapAsync(
        CancellationToken cancellationToken
    )
    {
        ValueTask<IReadOnlyList<CoinGeckoStablecoin>> coinsTask = this._cache.GetOrFetchAsync(
            key: CACHE_KEY_STABLECOINS,
            fetcher: this._coinGeckoClient.FetchStablecoinsAsync,
            typeInfo: AppJsonContext.Default.IReadOnlyListCoinGeckoStablecoin,
            cancellationToken: cancellationToken
        );

        ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>> coinListTask = this._cache.GetOrFetchAsync(
            key: CACHE_KEY_COIN_LIST,
            fetcher: this._coinGeckoClient.FetchCoinListAsync,
            typeInfo: AppJsonContext.Default.IReadOnlyListCoinGeckoCoinPlatforms,
            cancellationToken: cancellationToken
        );

        IReadOnlyList<CoinGeckoStablecoin> coins = await coinsTask;
        IReadOnlyList<CoinGeckoCoinPlatforms> coinList = await coinListTask;

        return DepegService.BuildStablecoinAddressMap(stablecoins: coins, coinList: coinList);
    }

    /// <summary>
    ///     Enriches filtered pools with hacks, depeg alerts, audit info, contract security,
    ///     access info, contract addresses, and URLs.
    ///     Excludes pools with depeg alerts.
    /// </summary>
    public async ValueTask<IReadOnlyList<Pool>> EnrichPoolsAsync(
        IReadOnlyList<RawPool> filteredPools,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyDictionary<string, List<HackInfo>> hackMap = await this.GetHackMapAsync(cancellationToken);
        IReadOnlyDictionary<string, AuditInfo> protocolAuditMap = await this.GetProtocolAuditMapAsync(
            cancellationToken
        );
        IReadOnlyDictionary<string, decimal> priceMap = await this.GetStablecoinPriceMapAsync(cancellationToken);
        IReadOnlyDictionary<string, string> addressMap = await this.GetStablecoinAddressMapAsync(cancellationToken);

        List<Pool> result = [];

        foreach (RawPool pool in filteredPools)
        {
            Pool? enriched = await this.TryEnrichPoolAsync(
                pool: pool,
                hackMap: hackMap,
                protocolAuditMap: protocolAuditMap,
                priceMap: priceMap,
                addressMap: addressMap,
                cancellationToken: cancellationToken
            );

            if (enriched is not null)
            {
                result.Add(enriched);
            }
        }

        return result;
    }

    private async ValueTask<Pool?> TryEnrichPoolAsync(
        RawPool pool,
        IReadOnlyDictionary<string, List<HackInfo>> hackMap,
        IReadOnlyDictionary<string, AuditInfo> protocolAuditMap,
        IReadOnlyDictionary<string, decimal> priceMap,
        IReadOnlyDictionary<string, string> addressMap,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<DepegAlert> depegAlerts = DepegService.CheckDepeg(
            poolSymbol: pool.Symbol,
            priceMap: priceMap,
            underlyingTokens: pool.UnderlyingTokens,
            addressMap: addressMap
        );

        if (depegAlerts.Count > 0)
        {
            return null;
        }

        IReadOnlyList<HackInfo> hacks = HacksService.MatchHacks(projectSlug: pool.Project, hackMap: hackMap);
        AuditInfo? auditInfo = ProtocolsService.MatchAuditInfo(
            projectSlug: pool.Project,
            protocolMap: protocolAuditMap
        );
        PoolAccessInfo accessInfo = PoolAccessService.DerivePoolAccessInfo(
            project: pool.Project,
            poolMeta: pool.PoolMeta
        );
        string[] contractAddresses = ContractAddressUtils.BuildContractAddresses(pool);
        Uri? url = PoolUrlService.GetPoolUrl(pool);

        IReadOnlyList<ContractSecurityInfo> contractSecurity =
            await this._contractSecurityService.GetContractSecurityForAddressesAsync(
                chain: pool.Chain,
                addresses: contractAddresses,
                cancellationToken: cancellationToken
            );

        return BuildPool(
            pool: pool,
            hacks: hacks,
            auditInfo: auditInfo,
            accessInfo: accessInfo,
            contractAddresses: contractAddresses,
            url: url,
            contractSecurity: contractSecurity
        );
    }

    private static Pool BuildPool(
        RawPool pool,
        IReadOnlyList<HackInfo> hacks,
        AuditInfo? auditInfo,
        PoolAccessInfo accessInfo,
        string[] contractAddresses,
        Uri? url,
        IReadOnlyList<ContractSecurityInfo> contractSecurity
    )
    {
        Predictions predictions = new()
        {
            PredictedClass = pool.Predictions?.PredictedClass,
            PredictedProbability = pool.Predictions?.PredictedProbability,
            BinnedConfidence = pool.Predictions?.BinnedConfidence,
        };

        return new Pool
        {
            Chain = pool.Chain,
            Project = pool.Project,
            Symbol = pool.Symbol,
            DataSource = string.Equals(a: pool.Project, b: "pendle", comparisonType: StringComparison.OrdinalIgnoreCase)
                ? "pendle"
                : "defillama",
            TvlUsd = pool.TvlUsd,
            ApyBase = pool.ApyBase,
            ApyReward = pool.ApyReward,
            Apy = pool.Apy,
            RewardTokens = pool.RewardTokens,
            PoolId = pool.PoolId,
            ApyPct1D = pool.ApyPct1D,
            ApyPct7D = pool.ApyPct7D,
            ApyPct30D = pool.ApyPct30D,
            Stablecoin = pool.Stablecoin,
            IlRisk = pool.IlRisk,
            Exposure = pool.Exposure,
            Predictions = predictions,
            PoolMeta = pool.PoolMeta,
            Mu = pool.Mu,
            Sigma = pool.Sigma,
            Count = pool.Count,
            Outlier = pool.Outlier,
            UnderlyingTokens = pool.UnderlyingTokens,
            Il7d = pool.Il7d,
            ApyBase7d = pool.ApyBase7d,
            ApyMean30d = pool.ApyMean30d,
            VolumeUsd1d = pool.VolumeUsd1d,
            VolumeUsd7d = pool.VolumeUsd7d,
            ApyBaseInception = pool.ApyBaseInception,
            Hacks = [.. hacks],
            DepegAlerts = [],
            AuditInfo = auditInfo,
            ContractSecurity = [.. contractSecurity],
            AccessInfo = accessInfo,
            ContractAddresses = contractAddresses,
            Url = url,
        };
    }
}
