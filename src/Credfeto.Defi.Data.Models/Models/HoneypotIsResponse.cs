using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("isHoneypot={HoneypotResult?.IsHoneypot} simulationSuccess={SimulationSuccess}")]
public sealed record HoneypotIsResponse
{
    [JsonPropertyName("simulationSuccess")]
    public bool? SimulationSuccess { get; init; }

    [JsonPropertyName("honeypotResult")]
    public HoneypotIsHoneypotResult? HoneypotResult { get; init; }

    [JsonPropertyName("simulationResult")]
    public HoneypotIsSimulationResult? SimulationResult { get; init; }
}
