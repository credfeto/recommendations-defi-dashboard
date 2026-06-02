using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Top-level response from the GoPlus token security API.
/// </summary>
[DebuggerDisplay("Code={Code} Count={Result?.Count}")]
public sealed record GoPlusResponse
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("result")]
    public IReadOnlyDictionary<string, GoPlusTokenResult>? Result { get; init; }
}
