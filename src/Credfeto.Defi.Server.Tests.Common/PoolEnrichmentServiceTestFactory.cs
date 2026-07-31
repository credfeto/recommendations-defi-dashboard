using System.Net.Http;
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

    private readonly ContractSecurityCacheService _securityCache;

    public PoolEnrichmentServiceTestFactory()
    {
        FakeDatabase database = new();
        this._securityCache = new ContractSecurityCacheService(database: database, timeProvider: this._timeProvider);
    }

    public PoolEnrichmentService CreateEnrichmentService(
        HttpMessageHandler httpHandler,
        IDefiLlamaPoolStorage? poolStorage = null,
        IDefiLlamaProtocolStorageService? protocolStorage = null,
        IDefiLlamaHackStorageService? hackStorage = null,
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
        hackStorage ??= new FakeDefiLlamaHackStorage();
        chainlinkStorage ??= new FakeChainlinkStorage();
        pendleStorage ??= new FakePendleStorage();
        coinGeckoStorage ??= new FakeCoinGeckoCoinStorage();
        coinGeckoStablecoinStorage ??= new FakeCoinGeckoStablecoinStorage();

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
            hackStorage: hackStorage,
            protocolStorage: protocolStorage,
            chainlinkStorage: chainlinkStorage,
            coinGeckoStorage: coinGeckoStorage,
            coinGeckoStablecoinStorage: coinGeckoStablecoinStorage,
            contractSecurityService: contractSecurity,
            poolStorage: poolStorage,
            pendleStorage: pendleStorage
        );
    }
}
