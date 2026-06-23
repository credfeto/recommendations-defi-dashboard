using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Storage;

public interface IDefiLlamaPoolStorage
{
    ValueTask StorePoolsAsync(
        IReadOnlyList<RawPool> pools,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    ValueTask<IReadOnlyList<RawPool>> GetAllPoolsAsync(CancellationToken cancellationToken);
}
