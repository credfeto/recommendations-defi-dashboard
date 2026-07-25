using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Storage;

public interface ICoinGeckoStablecoinStorageService
{
    ValueTask StoreAsync(
        IReadOnlyList<CoinGeckoStablecoin> stablecoins,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    ValueTask<IReadOnlyList<CoinGeckoStablecoin>> GetAllAsync(CancellationToken cancellationToken);
}
