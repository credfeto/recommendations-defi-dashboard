using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Tests;

[DebuggerDisplay("{CommandText}: {Parameters.Count} parameters")]
internal sealed record FakeDbExecution(string CommandText, IReadOnlyList<DbParameter> Parameters);
