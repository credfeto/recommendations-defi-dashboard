using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.GoPlus.Interfaces;
using Credfeto.Defi.ApiClients.HoneypotIs.Interfaces;
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
            .Services.AddOptions<CoinGeckoOptions>()
            .Services.AddStorage()
            .AddDefiBusinessServices();
    }

    [Fact]
    public async Task CoinGeckoHttpClientWithApiKeySendsUserAgentAndDemoApiKeyAsync()
    {
        await using ServiceProvider serviceProvider = BuildCoinGeckoServiceProvider("demo-key");
        using HttpClient client = CreateCoinGeckoHttpClient(serviceProvider);

        Assert.NotEmpty(client.DefaultRequestHeaders.UserAgent);
        Assert.True(
            client.DefaultRequestHeaders.TryGetValues(
                name: CoinGeckoHttpClientConfiguration.DEMO_PLAN_HEADER,
                values: out IEnumerable<string>? values
            ),
            userMessage: "Demo API key header should be present"
        );
        Assert.Equal(expected: "demo-key", actual: Assert.Single(values));
    }

    [Fact]
    public async Task CoinGeckoHttpClientWithoutApiKeySendsUserAgentOnlyAsync()
    {
        await using ServiceProvider serviceProvider = BuildCoinGeckoServiceProvider(string.Empty);
        using HttpClient client = CreateCoinGeckoHttpClient(serviceProvider);

        Assert.NotEmpty(client.DefaultRequestHeaders.UserAgent);
        Assert.False(
            client.DefaultRequestHeaders.Contains(CoinGeckoHttpClientConfiguration.DEMO_PLAN_HEADER),
            userMessage: "Demo API key header should be absent"
        );
    }

    private static ServiceProvider BuildCoinGeckoServiceProvider(string apiKey)
    {
        ServiceCollection services = new();
        _ = services
            .AddOptions<CoinGeckoOptions>()
            .Configure(opts => opts.ApiKey = apiKey)
            .Services.AddDefiBusinessServices();

        return services.BuildServiceProvider();
    }

    private static HttpClient CreateCoinGeckoHttpClient(ServiceProvider serviceProvider)
    {
        return serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(CoinGeckoStablecoinsClient));
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
    public void DefiLlamaStablecoinsClientIsRegistered()
    {
        this.RequireService<IDefiLlamaStablecoinsClient>();
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
    public void HoneypotIsClientIsRegistered()
    {
        this.RequireService<IHoneypotIsClient>();
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
