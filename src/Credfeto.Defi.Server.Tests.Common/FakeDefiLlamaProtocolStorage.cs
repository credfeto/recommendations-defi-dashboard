using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakeDefiLlamaProtocolStorage : IDefiLlamaProtocolStorageService
{
    private readonly IReadOnlyList<RawProtocol> _protocols;

    public FakeDefiLlamaProtocolStorage()
        : this([]) { }

    public FakeDefiLlamaProtocolStorage(IReadOnlyList<RawProtocol> protocols)
    {
        this._protocols = protocols;
    }

    public ValueTask StoreProtocolsAsync(
        IReadOnlyList<RawProtocol> protocols,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<RawProtocol>> GetAllProtocolsAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._protocols);
}
