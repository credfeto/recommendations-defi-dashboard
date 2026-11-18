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
    public void AddDefiServices_MissingDatabaseConnectionString_FailsStartupValidation()
    {
        using WebApplication app = TestAppFactory.BuildApp(connectionString: string.Empty);

        IStartupValidator validator = app.Services.GetRequiredService<IStartupValidator>();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(validator.Validate);

        Assert.Equal(expected: typeof(DatabaseConfiguration), actual: exception.OptionsType);
    }

    [Fact]
    public void AddDefiServices_ValidDatabaseConnectionString_PassesStartupValidation()
    {
        using WebApplication app = TestAppFactory.BuildApp();

        IStartupValidator validator = app.Services.GetRequiredService<IStartupValidator>();

        Exception? exception = Record.Exception(validator.Validate);

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
