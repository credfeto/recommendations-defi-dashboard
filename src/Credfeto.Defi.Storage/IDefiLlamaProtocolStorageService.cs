using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Storage;

public interface IDefiLlamaProtocolStorageService
{
    ValueTask StoreProtocolsAsync(
        IReadOnlyList<RawProtocol> protocols,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    ValueTask<IReadOnlyList<RawProtocol>> GetAllProtocolsAsync(CancellationToken cancellationToken);
}
