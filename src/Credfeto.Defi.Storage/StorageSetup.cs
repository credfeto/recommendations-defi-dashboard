using System;
using Credfeto.Database.SqlServer;
using Credfeto.Defi.Storage.Configuration;
using Credfeto.Services.Startup.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Storage;

public static class StorageSetup
{
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IValidateOptions<DatabaseConfiguration>, DatabaseConfigurationValidator>();
        services.AddSingleton<IOptions<SqlServerConfiguration>>(sp =>
        {
            DatabaseConfiguration cfg = sp.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;

            return Options.Create(new SqlServerConfiguration(cfg.ConnectionString));
        });

        return services
            .AddSqlServer()
            .AddRunOnStartupTask<DatabaseMigrationService>()
            .AddSingleton<ApiCacheService>()
            .AddSingleton<ContractSecurityCacheService>();
    }
}
