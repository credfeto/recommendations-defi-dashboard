using System;
using Credfeto.Defi.Server.ApiClients.CoinGecko;
using Credfeto.Defi.Server.ApiClients.DefiLlama;
using Credfeto.Defi.Server.ApiClients.GoPlus;
using Credfeto.Defi.Server.ApiClients.Pendle;
using Credfeto.Defi.Server.Cache;
using Credfeto.Defi.Server.Config;
using Credfeto.Defi.Server.Json;
using Credfeto.Defi.Server.Mcp;
using Credfeto.Defi.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Defi.Server;

/// <summary>
///     Registers all application services.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///     Adds all DeFi Dashboard services to the dependency injection container.
    /// </summary>
    public static WebApplicationBuilder AddDefiServices(this WebApplicationBuilder builder)
    {
        _ = builder
            .Services.Configure<CacheConfig>(builder.Configuration.GetSection("Cache"))
            .Configure<RpcConfig>(builder.Configuration.GetSection("Rpc"))
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ApiCacheService>()
            .AddSingleton<ContractSecurityCacheService>()
            .AddHttpClient()
            .AddSingleton<DefiLlamaPoolsClient>()
            .AddSingleton<DefiLlamaHacksClient>()
            .AddSingleton<DefiLlamaProtocolsClient>()
            .AddSingleton<CoinGeckoStablecoinsClient>()
            .AddSingleton<PendleMarketsClient>()
            .AddSingleton<GoPlusClient>()
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
