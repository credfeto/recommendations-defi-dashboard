using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Defi.Mcp;

/// <summary>
///     Registers and configures the MCP endpoint using ModelContextProtocol.AspNetCore.
/// </summary>
public static class McpSetup
{
    /// <summary>
    ///     Adds MCP services and tool handlers to the service collection.
    /// </summary>
    public static IServiceCollection AddMcpTools(this IServiceCollection services)
    {
        _ = services.AddMcpServer().WithHttpTransport().WithTools<DefiMcpTools>();

        return services;
    }

    /// <summary>
    ///     Maps the /mcp endpoint on the application.
    /// </summary>
    public static void MapMcpEndpoint(this WebApplication app)
    {
        _ = app.MapMcp("/mcp");
    }
}
