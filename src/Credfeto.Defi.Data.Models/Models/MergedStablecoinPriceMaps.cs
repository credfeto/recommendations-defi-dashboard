using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("prices={Prices.Count}")]
public sealed record MergedStablecoinPriceMaps(
    IReadOnlyDictionary<string, decimal> Prices,
    IReadOnlyDictionary<string, StablecoinMetadata> Metadata
);
