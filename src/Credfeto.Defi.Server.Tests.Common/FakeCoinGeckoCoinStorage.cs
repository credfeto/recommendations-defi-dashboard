using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakeCoinGeckoCoinStorage : ICoinGeckoCoinStorageService
{
    private readonly IReadOnlyList<CoinGeckoCoinPlatforms> _coins;

    public FakeCoinGeckoCoinStorage()
        : this([]) { }

    public FakeCoinGeckoCoinStorage(IReadOnlyList<CoinGeckoCoinPlatforms> coins)
    {
        this._coins = coins;
    }

    public ValueTask StoreAsync(
        IReadOnlyList<CoinGeckoCoinPlatforms> coins,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>> GetAllAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._coins);
}
