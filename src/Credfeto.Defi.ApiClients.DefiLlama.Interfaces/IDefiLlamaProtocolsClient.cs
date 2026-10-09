using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.DefiLlama.Interfaces;

public interface IDefiLlamaProtocolsClient
{
    ValueTask<IReadOnlyList<RawProtocol>> FetchProtocolsAsync(CancellationToken cancellationToken);
}
