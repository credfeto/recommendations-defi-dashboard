using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Mcp;
using Credfeto.Defi.Server.Composition;
using Credfeto.Defi.Storage;
using Credfeto.Defi.Storage.Configuration;
using Credfeto.Services.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Defi.Server;

internal static class ServiceRegistration
{
    public static WebApplicationBuilder AddDefiServices(this WebApplicationBuilder builder)
    {
        _ = builder.Services.Configure<DatabaseConfiguration>(
            builder.Configuration.GetSection("DatabaseConfiguration")
        );

        _ = builder.Services.AddOptions<RpcConfig>().BindConfiguration("Rpc");

        _ = builder
            .Services.AddStorage()
            .AddRunOnStartupServices()
            .AddDefiBusinessServices()
            .AddMcpTools()
            .ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.TypeInfoResolverChain.Insert(index: 0, item: AppJsonContext.Default)
            );

        return builder;
    }
}
