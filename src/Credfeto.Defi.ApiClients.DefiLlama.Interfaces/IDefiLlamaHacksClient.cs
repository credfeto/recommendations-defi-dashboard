using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.DefiLlama.Interfaces;

/// <summary>
///     Fetches recorded DeFi exploit data from the DefiLlama hacks API.
/// </summary>
public interface IDefiLlamaHacksClient
{
    /// <summary>
    ///     Fetches all recorded DeFi exploits from DefiLlama.
    /// </summary>
    ValueTask<IReadOnlyList<RawHack>> FetchHacksAsync(CancellationToken cancellationToken);
}
