using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.HoneypotIs.Interfaces;

public interface IHoneypotIsClient
{
    ValueTask<IReadOnlyDictionary<string, HoneypotIsResult>> FetchTokenSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    );
}
