using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.GoPlus.Interfaces;
using Credfeto.Defi.ApiClients.Pendle.Interfaces;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using Credfeto.Defi.Storage.Configuration;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Credfeto.Defi.Server.Composition.Tests;

public sealed class ServiceCollectionExtensionsTests : DependencyInjectionTestsBase
{
    public ServiceCollectionExtensionsTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: ConfigureServices) { }

    private static IServiceCollection ConfigureServices(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddOptions<DatabaseConfiguration>()
            .Configure(opts => opts.ConnectionString = TestConnectionStrings.FakeSqlServer)
            .Services.AddOptions<RpcConfig>()
            .Services.AddStorage()
            .AddDefiBusinessServices();
    }

    [Fact]
    public void DefiLlamaPoolsClientIsRegistered()
    {
        this.RequireService<IDefiLlamaPoolsClient>();
    }

    [Fact]
    public void DefiLlamaHacksClientIsRegistered()
    {
        this.RequireService<IDefiLlamaHacksClient>();
    }

    [Fact]
    public void DefiLlamaProtocolsClientIsRegistered()
    {
        this.RequireService<IDefiLlamaProtocolsClient>();
    }

    [Fact]
    public void CoinGeckoStablecoinsClientIsRegistered()
    {
        this.RequireService<ICoinGeckoStablecoinsClient>();
    }

    [Fact]
    public void ChainlinkStablecoinsClientIsRegistered()
    {
        this.RequireService<IChainlinkStablecoinsClient>();
    }

    [Fact]
    public void PendleMarketsClientIsRegistered()
    {
        this.RequireService<IPendleMarketsClient>();
    }

    [Fact]
    public void GoPlusClientIsRegistered()
    {
        this.RequireService<IGoPlusClient>();
    }

    [Fact]
    public void ProxyResolverServiceIsRegistered()
    {
        this.RequireService<ProxyResolverService>();
    }

    [Fact]
    public void ContractSecurityServiceIsRegistered()
    {
        this.RequireService<ContractSecurityService>();
    }

    [Fact]
    public void PoolEnrichmentServiceIsRegistered()
    {
        this.RequireService<PoolEnrichmentService>();
    }

    [Fact]
    public void CacheWarmerServiceIsRegisteredAsHostedService()
    {
        this.RequireServiceInCollectionFor<IHostedService, CacheWarmerService>();
    }
}
