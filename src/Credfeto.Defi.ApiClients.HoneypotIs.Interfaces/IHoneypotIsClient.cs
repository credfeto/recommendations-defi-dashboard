using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.HoneypotIs.Interfaces;

/// <summary>
///     Fetches contract security information from the Honeypot.is API.
/// </summary>
public interface IHoneypotIsClient
{
    /// <summary>
    ///     Fetches security information for one or more contract addresses on a given chain.
    /// </summary>
    ValueTask<IReadOnlyDictionary<string, HoneypotIsResult>> FetchTokenSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    );
}
