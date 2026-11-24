using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.Webacy;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using Credfeto.Defi.Storage.Configuration;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Fact]
    public void AddDefiServices_BindsCoinGeckoOptionsFromConfiguration()
    {
        using WebApplication app = TestAppFactory.BuildApp();

        IOptions<CoinGeckoOptions> options = app.Services.GetRequiredService<IOptions<CoinGeckoOptions>>();

        Assert.Equal(expected: TestAppFactory.TEST_COINGECKO_API_KEY, actual: options.Value.ApiKey);
    }

    [Fact]
    public void AddDefiServices_BindsWebacyOptionsFromConfiguration()
    {
        using WebApplication app = TestAppFactory.BuildApp();

        IOptions<WebacyOptions> options = app.Services.GetRequiredService<IOptions<WebacyOptions>>();

        Assert.Equal(expected: TestAppFactory.TEST_WEBACY_API_KEY, actual: options.Value.ApiKey);
    }

    [Fact]
    public async Task AddDefiServices_MissingDatabaseConnectionString_FailsStartupValidationAsync()
    {
        await using WebApplication app = TestAppFactory.BuildApp(connectionString: string.Empty);

        IAsyncStartupValidator validator = app.Services.GetRequiredService<IAsyncStartupValidator>();

        OptionsValidationException exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            validator.ValidateAsync(this.CancellationToken())
        );

        Assert.Equal(expected: typeof(DatabaseConfiguration), actual: exception.OptionsType);
    }

    [Fact]
    public async Task AddDefiServices_ValidDatabaseConnectionString_PassesStartupValidationAsync()
    {
        await using WebApplication app = TestAppFactory.BuildApp();

        IAsyncStartupValidator validator = app.Services.GetRequiredService<IAsyncStartupValidator>();

        Exception? exception = await Record.ExceptionAsync(() => validator.ValidateAsync(this.CancellationToken()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AddDefiServices_MissingDatabaseConnectionString_FailsHostStartupAsync()
    {
        await using WebApplication app = TestAppFactory.BuildApp(connectionString: string.Empty);

        OptionsValidationException exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            app.StartAsync(this.CancellationToken())
        );

        Assert.Equal(expected: typeof(DatabaseConfiguration), actual: exception.OptionsType);
    }
}
