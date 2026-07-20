using Credfeto.Defi.Server.Composition;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Credfeto.Defi.Server.Endpoints;

internal static class HealthEndpoints
{
    public static void MapHealthCheck(this IEndpointRouteBuilder app)
    {
        // Wrapped in a lambda rather than passed as a bare method group: referencing a
        // Credfeto.Defi.Server.Composition method group directly here crashes the built-in
        // RouteHandlerAnalyzer with AD0001 (IndexOutOfRangeException) during build.
        _ = app.MapGet(pattern: "/ping", handler: static () => HealthEndpointHandlers.Ping())
            .WithName("Ping")
            .Produces<string>();
    }
}
