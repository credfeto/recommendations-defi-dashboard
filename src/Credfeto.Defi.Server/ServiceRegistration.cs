using System;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.GoPlus;
using Credfeto.Defi.ApiClients.GoPlus.Interfaces;
using Credfeto.Defi.ApiClients.Pendle;
using Credfeto.Defi.ApiClients.Pendle.Interfaces;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Database;
using Credfeto.Defi.Mcp;
using Credfeto.Defi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Defi.Server;

/// <summary>
///     Registers all application services.
/// </summary>
internal static class ServiceRegistration
{
    /// <summary>
    ///     Adds all DeFi Dashboard services to the dependency injection container.
    /// </summary>
    public static WebApplicationBuilder AddDefiServices(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddOptions<CacheConfig>().BindConfiguration("Cache");

        _ = builder.Services.AddOptions<RpcConfig>().BindConfiguration("Rpc");

        _ = builder
            .Services.AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ApiCacheService>()
            .AddSingleton<ContractSecurityCacheService>()
            .AddHttpClient()
            .AddSingleton<IDefiLlamaPoolsClient, DefiLlamaPoolsClient>()
            .AddSingleton<IDefiLlamaHacksClient, DefiLlamaHacksClient>()
            .AddSingleton<IDefiLlamaProtocolsClient, DefiLlamaProtocolsClient>()
            .AddSingleton<ICoinGeckoStablecoinsClient, CoinGeckoStablecoinsClient>()
            .AddSingleton<IPendleMarketsClient, PendleMarketsClient>()
            .AddSingleton<IGoPlusClient, GoPlusClient>()
            .AddSingleton<ProxyResolverService>()
            .AddSingleton<ContractSecurityService>()
            .AddSingleton<PoolEnrichmentService>()
            .AddHostedService<CacheWarmerService>()
            .AddMcpTools()
            .ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.TypeInfoResolverChain.Insert(index: 0, item: AppJsonContext.Default)
            );

        return builder;
    }
}
