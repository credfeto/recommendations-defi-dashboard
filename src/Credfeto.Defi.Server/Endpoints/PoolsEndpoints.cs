using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Credfeto.Defi.Server.Endpoints;

/// <summary>
///     Maps the /api/pools REST endpoints.
/// </summary>
internal static class PoolsEndpoints
{
    private const string CACHE_CONTROL = "public, max-age=15, s-maxage=15, stale-while-revalidate=5";

    /// <summary>
    ///     Registers pool-related endpoints on the application.
    /// </summary>
    public static void MapPoolsEndpoints(this IEndpointRouteBuilder app)
    {
        _ = app.MapGet(pattern: "/api/pools", handler: GetPoolTypes)
            .WithName("GetPoolTypes")
            .Produces<PoolTypeMetadata[]>();

        _ = app.MapGet(pattern: "/api/pools/{poolName}", handler: GetPoolsByNameAsync)
            .WithName("GetPoolsByName")
            .Produces<Pool[]>()
            .ProducesProblem(400);
    }

    private static IResult GetPoolTypes(HttpContext context)
    {
        context.Response.Headers.CacheControl = CACHE_CONTROL;

        return Results.Ok(PoolTypeService.GetAllPoolTypes());
    }

    private static async Task<IResult> GetPoolsByNameAsync(
        string poolName,
        PoolEnrichmentService enrichmentService,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        if (!PoolTypeService.IsValidPoolType(poolName))
        {
            return Results.BadRequest(
                new { error = $"Invalid pool name. Valid options: ETH, STABLES, HIGH_YIELD, LOW_TVL, BLUE_CHIP" }
            );
        }

        IReadOnlyList<RawPool> allPools = await enrichmentService.GetAllPoolsAsync(cancellationToken);
        IReadOnlyList<RawPool> filtered = PoolFilterService.FilterPoolsByType(allPools: allPools, poolType: poolName);
        IReadOnlyList<Pool> enriched = await enrichmentService.EnrichPoolsAsync(
            filteredPools: filtered,
            cancellationToken: cancellationToken
        );

        context.Response.Headers.CacheControl = CACHE_CONTROL;

        return Results.Ok(enriched);
    }
}
