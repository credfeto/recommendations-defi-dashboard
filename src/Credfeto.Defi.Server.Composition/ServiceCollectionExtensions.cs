using System;
using Credfeto.Defi.ApiClients.Chainlink;
using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.GoPlus;
using Credfeto.Defi.ApiClients.GoPlus.Interfaces;
using Credfeto.Defi.ApiClients.HoneypotIs;
using Credfeto.Defi.ApiClients.HoneypotIs.Interfaces;
using Credfeto.Defi.ApiClients.Pendle;
using Credfeto.Defi.ApiClients.Pendle.Interfaces;
using Credfeto.Defi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Server.Composition;

public static class ServiceCollectionExtensions
{
    // Callers must also call AddStorage() and bind RpcConfig and CoinGeckoOptions: the services
    // registered here depend on them but none is registered by this method.
    public static IServiceCollection AddDefiBusinessServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .AddHttpClient()
            .AddHttpClient(nameof(CoinGeckoStablecoinsClient))
            .ConfigureHttpClient(
                static (serviceProvider, client) =>
                    CoinGeckoHttpClientConfiguration.Configure(
                        client: client,
                        options: serviceProvider.GetRequiredService<IOptions<CoinGeckoOptions>>().Value
                    )
            )
            .Services.AddSingleton<IDefiLlamaPoolsClient, DefiLlamaPoolsClient>()
            .AddSingleton<IDefiLlamaHacksClient, DefiLlamaHacksClient>()
            .AddSingleton<IDefiLlamaProtocolsClient, DefiLlamaProtocolsClient>()
            .AddSingleton<IDefiLlamaStablecoinsClient, DefiLlamaStablecoinsClient>()
            .AddSingleton<ICoinGeckoStablecoinsClient, CoinGeckoStablecoinsClient>()
            .AddSingleton<IChainlinkStablecoinsClient, ChainlinkStablecoinsClient>()
            .AddSingleton<IPendleMarketsClient, PendleMarketsClient>()
            .AddSingleton<IGoPlusClient, GoPlusClient>()
            .AddSingleton<IHoneypotIsClient, HoneypotIsClient>()
            .AddSingleton<ProxyResolverService>()
            .AddSingleton<ContractSecurityService>()
            .AddSingleton<PoolEnrichmentService>()
            .AddHostedService<CacheWarmerService>();
    }
}
