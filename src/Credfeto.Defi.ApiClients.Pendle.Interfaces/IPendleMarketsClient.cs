using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.Pendle.Interfaces;

public interface IPendleMarketsClient
{
    ValueTask<IReadOnlyList<PendleMarket>> FetchMarketsAsync(CancellationToken cancellationToken);
}
