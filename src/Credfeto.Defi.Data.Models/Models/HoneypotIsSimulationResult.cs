using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("buyTax={BuyTax} sellTax={SellTax}")]
public sealed record HoneypotIsSimulationResult
{
    [JsonPropertyName("buyTax")]
    public double? BuyTax { get; init; }

    [JsonPropertyName("sellTax")]
    public double? SellTax { get; init; }
}
