using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Honeypot simulation verdict from the Honeypot.is API.
/// </summary>
[DebuggerDisplay("isHoneypot={IsHoneypot}")]
public sealed record HoneypotIsHoneypotResult
{
    [JsonPropertyName("isHoneypot")]
    public bool? IsHoneypot { get; init; }
}
