using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Key}: {FetchedAt}")]
internal sealed record ApiCacheRow(string Key, string Data, DateTimeOffset FetchedAt);
