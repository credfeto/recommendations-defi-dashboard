using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Services.Tests;

public sealed class PoolAccessServiceTests : TestBase
{
    [Theory]
    [InlineData("maple")]
    [InlineData("Maple-V2")]
    [InlineData("CENTRIFUGE")]
    public static void DerivePoolAccessInfo_KycProjectAnyCase_RequiresKyc(string project)
    {
        PoolAccessInfo result = PoolAccessService.DerivePoolAccessInfo(project: project, poolMeta: null);

        Assert.True(result.KycRequiredForEntry);
        Assert.True(result.KycRequiredForExit);
        Assert.Null(result.CanSwapToExit);
    }

    [Theory]
    [InlineData("uniswap-v3")]
    [InlineData("Uniswap-V3")]
    [InlineData("PENDLE")]
    public static void DerivePoolAccessInfo_SwapProjectAnyCase_CanSwapToExit(string project)
    {
        PoolAccessInfo result = PoolAccessService.DerivePoolAccessInfo(project: project, poolMeta: null);

        Assert.True(result.CanSwapToExit);
        Assert.Null(result.KycRequiredForEntry);
        Assert.Null(result.KycRequiredForExit);
    }

    [Fact]
    public void DerivePoolAccessInfo_UnknownProject_LeavesFlagsUnset()
    {
        PoolAccessInfo result = PoolAccessService.DerivePoolAccessInfo(project: "unknown-protocol", poolMeta: null);

        Assert.Null(result.CanSwapToExit);
        Assert.Null(result.KycRequiredForEntry);
        Assert.Null(result.KycRequiredForExit);
        Assert.Null(result.IsLiquid);
        Assert.Null(result.LockupDescription);
    }
}
