using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Storage.Configuration;
using Credfeto.Services.Startup.Interfaces;
using DbUp;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Storage;

public sealed class DatabaseMigrationService : IRunOnStartup
{
    private readonly IOptions<DatabaseConfiguration> _config;

    public DatabaseMigrationService(IOptions<DatabaseConfiguration> config)
    {
        this._config = config;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        string connectionString = this._config.Value.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return ValueTask.CompletedTask;
        }

        DbUp.Engine.DatabaseUpgradeResult result = DeployChanges
            .To.SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            throw new System.InvalidOperationException(
                $"Database migration failed: {result.Error?.Message}",
                result.Error
            );
        }

        return ValueTask.CompletedTask;
    }
}
