using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakeDatabase : IDatabase
{
    private readonly Dictionary<Type, Queue<object?>> _returnQueues = [];

    public FakeDatabase WithReturn<T>(T? value)
    {
        Type type = typeof(T);

        if (!this._returnQueues.TryGetValue(type, out Queue<object?>? queue))
        {
            queue = new Queue<object?>();
            this._returnQueues[type] = queue;
        }

        queue.Enqueue(value);

        return this;
    }

    public ValueTask<T> ExecuteAsync<T>(Func<DbConnection, CancellationToken, ValueTask<T>> action, CancellationToken cancellationToken)
    {
        Type type = typeof(T);

        if (this._returnQueues.TryGetValue(type, out Queue<object?>? queue) && queue.Count > 0)
        {
            object? rawValue = queue.Dequeue();

            return ValueTask.FromResult(rawValue == null ? default! : (T)rawValue);
        }

        return ValueTask.FromResult(default(T)!);
    }

    public ValueTask ExecuteAsync(Func<DbConnection, CancellationToken, ValueTask> action, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
