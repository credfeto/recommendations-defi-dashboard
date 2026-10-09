using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.ApiClients.CoinGecko.Interfaces;

public interface ICoinGeckoStablecoinsClient
{
    ValueTask<IReadOnlyList<CoinGeckoStablecoin>> FetchStablecoinsAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>> FetchCoinListAsync(CancellationToken cancellationToken);
}
