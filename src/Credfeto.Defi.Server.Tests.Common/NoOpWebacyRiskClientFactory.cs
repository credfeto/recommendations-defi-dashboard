using System;
using System.Collections.Generic;
using System.Threading;
using Credfeto.Defi.ApiClients.Webacy.Interfaces;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using NSubstitute;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class NoOpWebacyRiskClientFactory : TestBase
{
    public static IWebacyRiskClient Create()
    {
        IWebacyRiskClient client = GetSubstitute<IWebacyRiskClient>();
        client
            .FetchTokenRiskAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, WebacyResult>(StringComparer.OrdinalIgnoreCase));

        return client;
    }
}
