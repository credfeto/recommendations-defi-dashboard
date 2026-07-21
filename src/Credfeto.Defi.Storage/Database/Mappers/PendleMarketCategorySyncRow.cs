using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Address}/{ChainId} = {CategoryId}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in PendleMarketStorageService"
)]
internal sealed record PendleMarketCategorySyncRow(string Address, int ChainId, string CategoryId);
