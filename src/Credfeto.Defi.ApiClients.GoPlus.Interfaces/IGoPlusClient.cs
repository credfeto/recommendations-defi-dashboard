using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.GoPlus.Interfaces;

/// <summary>
///     Fetches contract security information from the GoPlus Labs API.
/// </summary>
public interface IGoPlusClient
{
    /// <summary>
    ///     Fetches security information for one or more contract addresses on a given chain.
    /// </summary>
    ValueTask<IReadOnlyDictionary<string, GoPlusTokenResult>> FetchTokenSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    );
}
