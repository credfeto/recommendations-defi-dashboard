using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Symbol} price={CurrentPrice}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in ChainlinkPriceFeedStorageService"
)]
internal sealed record ChainlinkPriceFeedSyncRow(string Symbol, decimal CurrentPrice);
