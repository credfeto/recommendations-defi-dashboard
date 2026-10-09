using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.Chainlink.Interfaces;

public interface IChainlinkStablecoinsClient
{
    ValueTask<IReadOnlyList<ChainlinkPriceFeed>> FetchStablecoinsAsync(CancellationToken cancellationToken);
}
