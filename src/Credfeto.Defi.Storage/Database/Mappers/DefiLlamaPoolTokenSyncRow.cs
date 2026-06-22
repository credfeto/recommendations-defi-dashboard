using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{PoolId}[{SortOrder}] = {TokenAddress}")]
internal sealed record DefiLlamaPoolTokenSyncRow(string PoolId, int SortOrder, string TokenAddress);
