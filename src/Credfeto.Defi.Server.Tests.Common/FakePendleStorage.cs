using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakePendleStorage : IPendleMarketStorageService
{
    private readonly IReadOnlyList<RawPool> _pools;

    public FakePendleStorage()
        : this([]) { }

    public FakePendleStorage(IReadOnlyList<RawPool> pools)
    {
        this._pools = pools;
    }

    public ValueTask StoreMarketsAsync(
        IReadOnlyList<PendleMarket> markets,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<RawPool>> GetAllPoolsAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._pools);
}
