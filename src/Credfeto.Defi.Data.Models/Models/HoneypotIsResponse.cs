using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Raw per-address response from the Honeypot.is simulation API.
/// </summary>
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
