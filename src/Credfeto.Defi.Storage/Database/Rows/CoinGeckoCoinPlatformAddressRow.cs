using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{CoinId} [{Platform}] = {ContractAddress}")]
public sealed record CoinGeckoCoinPlatformAddressRow(
    string CoinId,
    string Platform,
    string ContractAddress,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
