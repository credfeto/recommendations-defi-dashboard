using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Raw protocol entry from the DefiLlama protocols API.
/// </summary>
[DebuggerDisplay("{Slug}")]
public sealed record RawProtocol
{
    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("audits")]
    public string? Audits { get; init; }

    [JsonPropertyName("audit_links")]
    public string[]? AuditLinks { get; init; }
}
