using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakeDefiLlamaHackStorage : IDefiLlamaHackStorageService
{
    private readonly IReadOnlyList<RawHack> _hacks;

    public FakeDefiLlamaHackStorage()
        : this([]) { }

    public FakeDefiLlamaHackStorage(IReadOnlyList<RawHack> hacks)
    {
        this._hacks = hacks;
    }

    public ValueTask StoreHacksAsync(
        IReadOnlyList<RawHack> hacks,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<RawHack>> GetAllHacksAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._hacks);
}
