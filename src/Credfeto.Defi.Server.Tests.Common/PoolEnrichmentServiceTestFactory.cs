using System.Net.Http;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.ApiClients.GoPlus;
using Credfeto.Defi.ApiClients.Pendle;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using FunFair.Test.Common;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class PoolEnrichmentServiceTestFactory : TestBase
{
    public FakeTimeProvider TimeProvider { get; } = new();

    public ApiCacheService ApiCache { get; }

    public ContractSecurityCacheService SecurityCache { get; }

    public PoolEnrichmentServiceTestFactory()
    {
        FakeDatabase database = new();
        this.ApiCache = new ApiCacheService(database: database, timeProvider: this.TimeProvider);
        this.SecurityCache = new ContractSecurityCacheService(database: database, timeProvider: this.TimeProvider);
    }

    public PoolEnrichmentService CreateEnrichmentService(
        HttpMessageHandler httpHandler,
        IDefiLlamaPoolStorage? poolStorage = null,
        IChainlinkPriceFeedStorageService? chainlinkStorage = null
    )
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(httpHandler));

        poolStorage ??= new FakePoolStorage();
        chainlinkStorage ??= new FakeChainlinkStorage();

        PendleMarketsClient pendleClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<PendleMarketsClient>()
        );
        DefiLlamaHacksClient hacksClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<DefiLlamaHacksClient>()
        );
        DefiLlamaProtocolsClient protocolsClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<DefiLlamaProtocolsClient>()
        );
        CoinGeckoStablecoinsClient coinGeckoClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<CoinGeckoStablecoinsClient>()
        );
        GoPlusClient goPlusClient = new(httpClientFactory: factory, logger: this.GetTypedLogger<GoPlusClient>());

        IOptions<RpcConfig> rpcOptions = Options.Create(new RpcConfig());
        ProxyResolverService proxyResolver = new(
            rpcConfig: rpcOptions,
            httpClientFactory: factory,
            logger: this.GetTypedLogger<ProxyResolverService>()
        );

        ContractSecurityService contractSecurity = new(
            goPlusClient: goPlusClient,
            cache: this.SecurityCache,
            proxyResolver: proxyResolver
        );

        return new PoolEnrichmentService(
            pendleClient: pendleClient,
            hacksClient: hacksClient,
            protocolsClient: protocolsClient,
            coinGeckoClient: coinGeckoClient,
            chainlinkStorage: chainlinkStorage,
            contractSecurityService: contractSecurity,
            poolStorage: poolStorage,
            cache: this.ApiCache
        );
    }
}
