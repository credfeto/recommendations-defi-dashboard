using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Services;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Credfeto.Defi.Server.Composition.Tests;

public sealed class PoolsEndpointHandlersTests : TestBase
{
    private const string EMPTY_ARRAY = "[]";

    private readonly PoolEnrichmentServiceTestFactory _factory = new();

    [Fact]
    public void GetPoolTypes_ReturnsAllPoolTypesAndSetsCacheControl()
    {
        DefaultHttpContext context = new();

        IResult result = PoolsEndpointHandlers.GetPoolTypes(context);

        Ok<PoolTypeMetadata[]> ok = Assert.IsType<Ok<PoolTypeMetadata[]>>(result);
        Assert.Equal(expected: PoolTypeService.GetAllPoolTypes(), actual: ok.Value);
        Assert.Equal(
            expected: PoolsEndpointHandlers.CACHE_CONTROL,
            actual: context.Response.Headers.CacheControl.ToString()
        );
    }

    [Fact]
    public async Task GetPoolsByNameAsync_InvalidPoolName_ReturnsBadRequestAsync()
    {
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);
        PoolEnrichmentService service = this._factory.CreateEnrichmentService(httpHandler: handler);
        DefaultHttpContext context = new();

        IResult result = await PoolsEndpointHandlers.GetPoolsByNameAsync(
            poolName: "NOT_A_REAL_POOL_TYPE",
            enrichmentService: service,
            context: context,
            cancellationToken: this.CancellationToken()
        );

        IStatusCodeHttpResult statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.NotNull(statusCodeResult.StatusCode);
        Assert.Equal(expected: (int)HttpStatusCode.BadRequest, actual: statusCodeResult.StatusCode!.Value);
    }

    [Fact]
    public async Task GetPoolsByNameAsync_ValidPoolName_ReturnsOkWithCacheControlAsync()
    {
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);
        PoolEnrichmentService service = this._factory.CreateEnrichmentService(httpHandler: handler);
        DefaultHttpContext context = new();

        IResult result = await PoolsEndpointHandlers.GetPoolsByNameAsync(
            poolName: "STABLES",
            enrichmentService: service,
            context: context,
            cancellationToken: this.CancellationToken()
        );

        Ok<IReadOnlyList<Pool>> ok = Assert.IsType<Ok<IReadOnlyList<Pool>>>(result);
        Assert.NotNull(ok.Value);
        Assert.Empty(ok.Value);
        Assert.Equal(
            expected: PoolsEndpointHandlers.CACHE_CONTROL,
            actual: context.Response.Headers.CacheControl.ToString()
        );
    }
}
