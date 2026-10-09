using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("isHoneypot={IsHoneypot}")]
public sealed record HoneypotIsHoneypotResult
{
    [JsonPropertyName("isHoneypot")]
    public bool? IsHoneypot { get; init; }
}
