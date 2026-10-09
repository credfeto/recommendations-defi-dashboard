using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.GoPlus.Interfaces;

public interface IGoPlusClient
{
    ValueTask<IReadOnlyDictionary<string, GoPlusTokenResult>> FetchTokenSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    );
}
