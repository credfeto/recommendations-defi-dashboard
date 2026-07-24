using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Storage;

public interface ICoinGeckoCoinStorageService
{
    ValueTask StoreAsync(
        IReadOnlyList<CoinGeckoCoinPlatforms> coins,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>> GetAllAsync(CancellationToken cancellationToken);
}
