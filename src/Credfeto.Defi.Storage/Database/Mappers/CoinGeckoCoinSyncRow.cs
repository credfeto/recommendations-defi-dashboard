using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Id} ({Symbol})")]
internal sealed record CoinGeckoCoinSyncRow(string Id, string Symbol);
