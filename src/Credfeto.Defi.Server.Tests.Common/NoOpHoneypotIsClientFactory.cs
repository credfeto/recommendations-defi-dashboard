using System;
using System.Collections.Generic;
using System.Threading;
using Credfeto.Defi.ApiClients.HoneypotIs.Interfaces;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using NSubstitute;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class NoOpHoneypotIsClientFactory : TestBase
{
    public static IHoneypotIsClient Create()
    {
        IHoneypotIsClient client = GetSubstitute<IHoneypotIsClient>();
        client
            .FetchTokenSecurityAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, HoneypotIsResult>(StringComparer.OrdinalIgnoreCase));

        return client;
    }
}
