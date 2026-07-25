using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakeCoinGeckoStablecoinStorage : ICoinGeckoStablecoinStorageService
{
    private readonly IReadOnlyList<CoinGeckoStablecoin> _stablecoins;

    public FakeCoinGeckoStablecoinStorage()
        : this([]) { }

    public FakeCoinGeckoStablecoinStorage(IReadOnlyList<CoinGeckoStablecoin> stablecoins)
    {
        this._stablecoins = stablecoins;
    }

    public ValueTask StoreAsync(
        IReadOnlyList<CoinGeckoStablecoin> stablecoins,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<CoinGeckoStablecoin>> GetAllAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._stablecoins);
}
