using System.Collections.Generic;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ServiceRegistrationTests : TestBase
{
    [Fact]
    public void AddDefiServices_ResolvesBusinessServiceFromComposition()
    {
        using WebApplication app = TestAppFactory.BuildApp();

        PoolEnrichmentService service = app.Services.GetRequiredService<PoolEnrichmentService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddDefiServices_ResolvesMcpToolRegistration()
    {
        using WebApplication app = TestAppFactory.BuildApp();

        IEnumerable<McpServerTool> tools = app.Services.GetServices<McpServerTool>();

        Assert.NotEmpty(tools);
    }

    [Fact]
    public void AddDefiServices_ResolvesStorageService()
    {
        using WebApplication app = TestAppFactory.BuildApp();

        ApiCacheService cache = app.Services.GetRequiredService<ApiCacheService>();

        Assert.NotNull(cache);
    }
}
