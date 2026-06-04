using System.Threading.Tasks;
using Credfeto.Defi.Storage.Configuration;
using FunFair.Test.Common;
using Microsoft.Extensions.Options;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class DatabaseMigrationServiceTests : TestBase
{
    [Fact]
    public async Task StartAsync_WithEmptyConnectionString_CompletesWithoutMigratingAsync()
    {
        IOptions<DatabaseConfiguration> config = Options.Create(
            new DatabaseConfiguration { ConnectionString = string.Empty }
        );
        DatabaseMigrationService service = new(config);

        await service.StartAsync(cancellationToken: this.CancellationToken());
    }
}
