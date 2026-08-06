using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Chain}/{Address} isHoneypot={IsHoneypot}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in ContractSecurityCacheService"
)]
internal sealed record HoneypotIsTokenSecuritySyncRow(
    string Chain,
    string Address,
    bool? IsHoneypot,
    double? BuyTax,
    double? SellTax,
    bool? SimulationSuccess
);
