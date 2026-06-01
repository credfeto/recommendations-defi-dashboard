using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Credfeto.Defi.Server.Endpoints;

internal static class HealthEndpoints
{
    public static void MapHealthCheck(this IEndpointRouteBuilder app)
    {
        _ = app.MapGet(pattern: "/ping", handler: static () => Results.Ok("pong")).WithName("Ping").Produces<string>();
    }
}
