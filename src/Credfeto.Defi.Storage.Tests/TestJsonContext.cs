using System.Text.Json.Serialization;

namespace Credfeto.Defi.Storage.Tests;

[JsonSerializable(typeof(string))]
internal sealed partial class TestJsonContext : JsonSerializerContext { }
