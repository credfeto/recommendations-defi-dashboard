using System.Net.Http;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.ApiClients.GoPlus;
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
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly ApiCacheService _apiCache;

    private readonly ContractSecurityCacheService _securityCache;

    public PoolEnrichmentServiceTestFactory()
    {
        FakeDatabase database = new();
        this._apiCache = new ApiCacheService(database: database, timeProvider: this._timeProvider);
        this._securityCache = new ContractSecurityCacheService(database: database, timeProvider: this._timeProvider);
    }

    public PoolEnrichmentService CreateEnrichmentService(
        HttpMessageHandler httpHandler,
        IDefiLlamaPoolStorage? poolStorage = null,
        IDefiLlamaProtocolStorageService? protocolStorage = null,
        IChainlinkPriceFeedStorageService? chainlinkStorage = null,
        IPendleMarketStorageService? pendleStorage = null,
        ICoinGeckoCoinStorageService? coinGeckoStorage = null,
        ICoinGeckoStablecoinStorageService? coinGeckoStablecoinStorage = null
    )
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(httpHandler));

        poolStorage ??= new FakePoolStorage();
        protocolStorage ??= new FakeDefiLlamaProtocolStorage();
        chainlinkStorage ??= new FakeChainlinkStorage();
        pendleStorage ??= new FakePendleStorage();
        coinGeckoStorage ??= new FakeCoinGeckoCoinStorage();
        coinGeckoStablecoinStorage ??= new FakeCoinGeckoStablecoinStorage();

        DefiLlamaHacksClient hacksClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<DefiLlamaHacksClient>()
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
            cache: this._securityCache,
            proxyResolver: proxyResolver
        );

        return new PoolEnrichmentService(
            hacksClient: hacksClient,
            protocolStorage: protocolStorage,
            chainlinkStorage: chainlinkStorage,
            coinGeckoStorage: coinGeckoStorage,
            coinGeckoStablecoinStorage: coinGeckoStablecoinStorage,
            contractSecurityService: contractSecurity,
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            cache: this._apiCache
        );
    }
}
