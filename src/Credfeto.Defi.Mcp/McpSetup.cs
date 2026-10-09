using System.Text.Json;
using Credfeto.Defi.Data.Models.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Defi.Mcp;

public static class McpSetup
{
    public static IServiceCollection AddMcpTools(this IServiceCollection services)
    {
        JsonSerializerOptions serializerOptions = new(options: AppJsonContext.Default.Options);

        _ = services.AddMcpServer().WithHttpTransport().WithTools<DefiMcpTools>(serializerOptions: serializerOptions);

        return services;
    }

    public static void MapMcpEndpoint(this WebApplication app)
    {
        _ = app.MapMcp("/mcp");
    }
}
