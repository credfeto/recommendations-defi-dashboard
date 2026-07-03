using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Storage;

public interface IChainlinkPriceFeedStorageService
{
    ValueTask StoreAsync(
        IReadOnlyList<ChainlinkPriceFeed> feeds,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    ValueTask<IReadOnlyList<ChainlinkPriceFeed>> GetAllAsync(CancellationToken cancellationToken);
}
