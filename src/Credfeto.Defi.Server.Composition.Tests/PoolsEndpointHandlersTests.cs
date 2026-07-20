using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Credfeto.Defi.Server.Composition.Tests;

public sealed class PoolsEndpointHandlersTests : TestBase
{
    private const string CACHE_CONTROL = "public, max-age=15, s-maxage=15, stale-while-revalidate=5";

    private readonly ApiCacheService _apiCache;
    private readonly ContractSecurityCacheService _securityCache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly PoolEnrichmentServiceTestFactory _factory = new();

    public PoolsEndpointHandlersTests()
    {
        this._timeProvider = new FakeTimeProvider();
        FakeDatabase database = new();
        this._apiCache = new ApiCacheService(database: database, timeProvider: this._timeProvider);
        this._securityCache = new ContractSecurityCacheService(database: database, timeProvider: this._timeProvider);
    }

    private PoolEnrichmentService CreateEnrichmentService(HttpMessageHandler httpHandler)
    {
        return this._factory.CreateEnrichmentService(
            httpHandler: httpHandler,
            cache: this._apiCache,
            securityCache: this._securityCache
        );
    }

    [Fact]
    public void GetPoolTypes_ReturnsAllPoolTypesAndSetsCacheControl()
    {
        DefaultHttpContext context = new();

        IResult result = PoolsEndpointHandlers.GetPoolTypes(context);

        Ok<PoolTypeMetadata[]> ok = Assert.IsType<Ok<PoolTypeMetadata[]>>(result);
        Assert.Equal(expected: PoolTypeService.GetAllPoolTypes(), actual: ok.Value);
        Assert.Equal(expected: CACHE_CONTROL, actual: context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task GetPoolsByNameAsync_InvalidPoolName_ReturnsBadRequestAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);
        PoolEnrichmentService service = this.CreateEnrichmentService(handler);
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
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);
        PoolEnrichmentService service = this.CreateEnrichmentService(handler);
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
        Assert.Equal(expected: CACHE_CONTROL, actual: context.Response.Headers.CacheControl.ToString());
    }
}
