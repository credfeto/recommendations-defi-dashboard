using Credfeto.Defi.Storage.Configuration;
using Credfeto.Services.Startup.Interfaces;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: ConfigureServices) { }

    private static IServiceCollection ConfigureServices(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddOptions<DatabaseConfiguration>()
            .Configure(opts =>
                opts.ConnectionString =
                    "Server=(local);Database=defi_test;Integrated Security=true;Connection Timeout=1;"
            )
            .Services.AddStorage();
    }

    [Fact]
    public void ApiCacheServiceIsRegistered()
    {
        this.RequireService<ApiCacheService>();
    }

    [Fact]
    public void ContractSecurityCacheServiceIsRegistered()
    {
        this.RequireService<ContractSecurityCacheService>();
    }

    [Fact]
    public void DatabaseConfigurationValidatorIsRegistered()
    {
        this.RequireServiceInCollectionFor<IValidateOptions<DatabaseConfiguration>, DatabaseConfigurationValidator>();
    }

    [Fact]
    public void DatabaseMigrationServiceIsRegistered()
    {
        this.RequireServiceInCollectionFor<IRunOnStartup, DatabaseMigrationService>();
    }
}
