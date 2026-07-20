using System;
using System.Linq;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Endpoints;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class EndpointMappingTests : TestBase
{
    [Fact]
    public async Task MapHealthCheck_RegistersAndInvokesPingRoute()
    {
        await using WebApplication app = TestAppFactory.BuildApp();

        app.MapHealthCheck();

        RouteEndpoint endpoint = GetEndpoint(app: app, pattern: "/ping");

        DefaultHttpContext context = new() { RequestServices = app.Services };

        await endpoint.RequestDelegate!(context);

        Assert.Equal(expected: StatusCodes.Status200OK, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task MapPoolsEndpoints_RegistersAndInvokesGetPoolTypesRoute()
    {
        await using WebApplication app = TestAppFactory.BuildApp();

        app.MapPoolsEndpoints();

        RouteEndpoint endpoint = GetEndpoint(app: app, pattern: "/api/pools");

        DefaultHttpContext context = new() { RequestServices = app.Services };

        await endpoint.RequestDelegate!(context);

        Assert.Equal(expected: StatusCodes.Status200OK, actual: context.Response.StatusCode);
    }

    [Fact]
    public async Task MapPoolsEndpoints_RegistersAndInvokesGetPoolsByNameRoute()
    {
        await using WebApplication app = TestAppFactory.BuildApp();

        app.MapPoolsEndpoints();

        RouteEndpoint endpoint = GetEndpoint(app: app, pattern: "/api/pools/{poolName}");

        DefaultHttpContext context = new() { RequestServices = app.Services };
        context.Request.RouteValues["poolName"] = "INVALID";

        await endpoint.RequestDelegate!(context);

        Assert.Equal(expected: StatusCodes.Status400BadRequest, actual: context.Response.StatusCode);
    }

    private static RouteEndpoint GetEndpoint(WebApplication app, string pattern)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => StringComparer.Ordinal.Equals(endpoint.RoutePattern.RawText, pattern));
    }
}
