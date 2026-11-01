using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.Webacy.Interfaces;

public interface IWebacyRiskClient
{
    ValueTask<IReadOnlyDictionary<string, WebacyResult>> FetchTokenRiskAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    );
}
