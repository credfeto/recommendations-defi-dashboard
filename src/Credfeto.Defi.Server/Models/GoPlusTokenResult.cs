using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Security analysis result for a single token from GoPlus.
/// </summary>
[DebuggerDisplay("{TokenSymbol} isProxy={IsProxy} isHoneypot={IsHoneypot}")]
internal sealed record GoPlusTokenResult
{
    [JsonPropertyName("is_open_source")]
    public string? IsOpenSource { get; init; }

    [JsonPropertyName("is_honeypot")]
    public string? IsHoneypot { get; init; }

    [JsonPropertyName("is_proxy")]
    public string? IsProxy { get; init; }

    [JsonPropertyName("buy_tax")]
    public string? BuyTax { get; init; }

    [JsonPropertyName("sell_tax")]
    public string? SellTax { get; init; }

    [JsonPropertyName("transfer_tax")]
    public string? TransferTax { get; init; }

    [JsonPropertyName("cannot_buy")]
    public string? CannotBuy { get; init; }

    [JsonPropertyName("honeypot_with_same_creator")]
    public string? HoneypotWithSameCreator { get; init; }

    [JsonPropertyName("token_name")]
    public string? TokenName { get; init; }

    [JsonPropertyName("token_symbol")]
    public string? TokenSymbol { get; init; }
}
