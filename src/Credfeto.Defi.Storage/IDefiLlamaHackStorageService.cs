using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Storage;

public interface IDefiLlamaHackStorageService
{
    ValueTask StoreHacksAsync(
        IReadOnlyList<RawHack> hacks,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    ValueTask<IReadOnlyList<RawHack>> GetAllHacksAsync(CancellationToken cancellationToken);
}
