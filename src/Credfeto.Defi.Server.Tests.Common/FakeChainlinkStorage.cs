using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakeChainlinkStorage : IChainlinkPriceFeedStorageService
{
    private readonly IReadOnlyList<ChainlinkPriceFeed> _feeds;

    public FakeChainlinkStorage()
        : this([]) { }

    public FakeChainlinkStorage(IReadOnlyList<ChainlinkPriceFeed> feeds)
    {
        this._feeds = feeds;
    }

    public ValueTask StoreAsync(
        IReadOnlyList<ChainlinkPriceFeed> feeds,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<ChainlinkPriceFeed>> GetAllAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._feeds);
}
