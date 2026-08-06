using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Chain}/{Address} isHoneypot={IsHoneypot}")]
public sealed record HoneypotIsTokenSecurityRow(
    string Chain,
    string Address,
    bool? IsHoneypot,
    double? BuyTax,
    double? SellTax,
    bool? SimulationSuccess,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated
);
