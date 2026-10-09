using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("isHoneypot={IsHoneypot} simulationSuccess={SimulationSuccess}")]
public sealed record HoneypotIsResult
{
    public bool? IsHoneypot { get; init; }

    public double? BuyTax { get; init; }

    public double? SellTax { get; init; }

    public bool? SimulationSuccess { get; init; }
}
