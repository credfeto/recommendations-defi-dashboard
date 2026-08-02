using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Xunit;

namespace Credfeto.Defi.Mcp.Tests;

public sealed class McpSetupTests : TestBase
{
    [Fact]
    public void AddMcpTools_RegistersMcpServerTool()
    {
        ServiceCollection services = new();

        _ = services.AddMcpTools();

        using ServiceProvider provider = services.BuildServiceProvider();

        IEnumerable<McpServerTool> tools = provider.GetServices<McpServerTool>();

        Assert.NotEmpty(tools);
    }

    [Fact]
    public async Task MapMcpEndpoint_RegistersMcpRouteAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.Sources.Clear();
        _ = builder.Services.AddMcpTools();

        await using WebApplication app = builder.Build();

        app.MapMcpEndpoint();

        bool hasMcpEndpoint = ((IEndpointRouteBuilder)app)
            .DataSources.SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Any(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/mcp", StringComparison.Ordinal) == true);

        Assert.True(hasMcpEndpoint, userMessage: "Expected an endpoint registered under /mcp");
    }
}
