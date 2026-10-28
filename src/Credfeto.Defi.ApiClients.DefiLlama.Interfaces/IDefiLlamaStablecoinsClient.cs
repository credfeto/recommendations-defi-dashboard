using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.DefiLlama.Interfaces;

public interface IDefiLlamaStablecoinsClient
{
    ValueTask<IReadOnlyList<DefiLlamaStablecoin>> FetchStablecoinsAsync(CancellationToken cancellationToken);
}
