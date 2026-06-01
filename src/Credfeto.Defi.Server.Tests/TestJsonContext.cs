using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Tests;

[JsonSerializable(typeof(string))]
internal sealed partial class TestJsonContext : JsonSerializerContext { }
