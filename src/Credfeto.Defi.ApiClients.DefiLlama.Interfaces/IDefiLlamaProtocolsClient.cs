using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.DefiLlama.Interfaces;

/// <summary>
///     Fetches protocol metadata from the DefiLlama protocols API.
/// </summary>
public interface IDefiLlamaProtocolsClient
{
    /// <summary>
    ///     Fetches all protocol metadata from DefiLlama.
    /// </summary>
    ValueTask<IReadOnlyList<RawProtocol>> FetchProtocolsAsync(CancellationToken cancellationToken);
}
