using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{CoinId} [{Platform}] = {ContractAddress}")]
internal sealed record CoinGeckoCoinPlatformAddressSyncRow(string CoinId, string Platform, string ContractAddress);
