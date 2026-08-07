using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Buy/sell simulation tax outcome from the Honeypot.is API.
/// </summary>
[DebuggerDisplay("buyTax={BuyTax} sellTax={SellTax}")]
public sealed record HoneypotIsSimulationResult
{
    [JsonPropertyName("buyTax")]
    public double? BuyTax { get; init; }

    [JsonPropertyName("sellTax")]
    public double? SellTax { get; init; }
}
