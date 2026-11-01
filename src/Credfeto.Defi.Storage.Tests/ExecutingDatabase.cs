using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;

namespace Credfeto.Defi.Storage.Tests;

internal sealed class ExecutingDatabase : IDatabase
{
    public FakeDbConnection Connection { get; } = new();

    public ValueTask<T> ExecuteAsync<T>(
        Func<DbConnection, CancellationToken, ValueTask<T>> action,
        CancellationToken cancellationToken
    )
    {
        return action(arg1: this.Connection, arg2: cancellationToken);
    }

    public ValueTask ExecuteAsync(
        Func<DbConnection, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken
    )
    {
        return action(arg1: this.Connection, arg2: cancellationToken);
    }
}
