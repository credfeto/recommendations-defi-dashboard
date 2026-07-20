using System.Runtime.CompilerServices;

// Allows Credfeto.Defi.Server.Tests to exercise the internal ServiceRegistration and Endpoints
// classes directly (e.g. building a WebApplication via WebApplication.CreateSlimBuilder(...) and
// mapping/registering against it) without making them public.
[assembly: InternalsVisibleTo("Credfeto.Defi.Server.Tests")]
