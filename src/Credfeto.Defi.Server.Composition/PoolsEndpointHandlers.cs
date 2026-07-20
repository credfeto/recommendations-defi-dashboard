using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services;
using Microsoft.AspNetCore.Http;

namespace Credfeto.Defi.Server.Composition;

public static class PoolsEndpointHandlers
{
    private const string CACHE_CONTROL = "public, max-age=15, s-maxage=15, stale-while-revalidate=5";

    public static IResult GetPoolTypes(HttpContext context)
    {
        context.Response.Headers.CacheControl = CACHE_CONTROL;

        return Results.Ok(PoolTypeService.GetAllPoolTypes());
    }

    public static async Task<IResult> GetPoolsByNameAsync(
        string poolName,
        PoolEnrichmentService enrichmentService,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        if (!PoolTypeService.IsValidPoolType(poolName))
        {
            return Results.BadRequest(
                new { error = "Invalid pool name. Valid options: ETH, STABLES, HIGH_YIELD, LOW_TVL, BLUE_CHIP" }
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
