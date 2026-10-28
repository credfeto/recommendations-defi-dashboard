using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FakeDefiLlamaStablecoinsClient : IDefiLlamaStablecoinsClient
{
    private readonly IReadOnlyList<DefiLlamaStablecoin> _stablecoins;

    public FakeDefiLlamaStablecoinsClient()
        : this([]) { }

    public FakeDefiLlamaStablecoinsClient(IReadOnlyList<DefiLlamaStablecoin> stablecoins)
    {
        this._stablecoins = stablecoins;
    }

    public ValueTask<IReadOnlyList<DefiLlamaStablecoin>> FetchStablecoinsAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(this._stablecoins);
}
