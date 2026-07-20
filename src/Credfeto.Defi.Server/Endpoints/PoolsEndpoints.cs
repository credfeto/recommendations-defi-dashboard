using System.Threading;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Server.Composition;
using Credfeto.Defi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Credfeto.Defi.Server.Endpoints;

internal static class PoolsEndpoints
{
    public static void MapPoolsEndpoints(this IEndpointRouteBuilder app)
    {
        // Both handlers below are wrapped in a lambda rather than passed as a bare method
        // group: referencing a Credfeto.Defi.Server.Composition method group directly here
        // crashes the built-in RouteHandlerAnalyzer with AD0001 (IndexOutOfRangeException)
        // during build.
        _ = app.MapGet(pattern: "/api/pools", handler: static (HttpContext context) => PoolsEndpointHandlers.GetPoolTypes(context))
            .WithName("GetPoolTypes")
            .Produces<PoolTypeMetadata[]>();

        _ = app.MapGet(
                pattern: "/api/pools/{poolName}",
                handler: static (string poolName, PoolEnrichmentService enrichmentService, HttpContext context, CancellationToken cancellationToken) =>
                    PoolsEndpointHandlers.GetPoolsByNameAsync(
                        poolName: poolName,
                        enrichmentService: enrichmentService,
                        context: context,
                        cancellationToken: cancellationToken
                    )
            )
            .WithName("GetPoolsByName")
            .Produces<Pool[]>()
            .ProducesProblem(400);
    }
}
