using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakePoolStorage : IDefiLlamaPoolStorage
{
    private readonly IReadOnlyList<RawPool> _pools;

    public FakePoolStorage()
        : this([]) { }

    public FakePoolStorage(IReadOnlyList<RawPool> pools)
    {
        this._pools = pools;
    }

    public ValueTask StorePoolsAsync(
        IReadOnlyList<RawPool> pools,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<RawPool>> GetAllPoolsAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._pools);
}
