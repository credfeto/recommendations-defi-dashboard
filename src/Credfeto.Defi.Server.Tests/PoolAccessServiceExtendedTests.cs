using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PoolAccessServiceExtendedTests : TestBase
{
    [Theory]
    [InlineData("uniswap-v2")]
    [InlineData("uniswap-v4")]
    [InlineData("balancer-v2")]
    [InlineData("balancer-v3")]
    [InlineData("sushiswap")]
    [InlineData("pancakeswap")]
    [InlineData("aerodrome")]
    [InlineData("velodrome")]
    [InlineData("camelot")]
    [InlineData("ramses")]
    [InlineData("thena")]
    [InlineData("trader-joe")]
    [InlineData("quickswap")]
    [InlineData("orca")]
    [InlineData("raydium")]
    public void DerivePoolAccessInfo_AdditionalSwapExitProjects_CanSwapToExit(string project)
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(project: project, poolMeta: null);

        Assert.True(info.CanSwapToExit);
    }

    [Fact]
    public void DerivePoolAccessInfo_LockupCooldownMeta_ReturnsLockupDescription()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "someprotocol",
            poolMeta: "7 day cooldown for withdrawals"
        );

        Assert.NotNull(info.LockupDescription);
        Assert.Contains(
            expectedSubstring: "cooldown",
            actualString: info.LockupDescription!,
            System.StringComparison.OrdinalIgnoreCase
        );
        Assert.False(info.IsLiquid);
    }

    [Fact]
    public void DerivePoolAccessInfo_UnstakingCooldownMeta_ReturnsLockupDescription()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "someprotocol",
            poolMeta: "unstaking cooldown: 21 days"
        );

        Assert.NotNull(info.LockupDescription);
        Assert.Contains(
            expectedSubstring: "unstaking cooldown",
            actualString: info.LockupDescription!,
            System.StringComparison.OrdinalIgnoreCase
        );
        Assert.False(info.IsLiquid);
    }

    [Fact]
    public void DerivePoolAccessInfo_WithdrawalCycleMeta_ReturnsLockupDescription()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "someprotocol",
            poolMeta: "7d unlock cycle for positions"
        );

        Assert.NotNull(info.LockupDescription);
        Assert.Contains(
            expectedSubstring: "withdrawal cycle",
            actualString: info.LockupDescription!,
            System.StringComparison.OrdinalIgnoreCase
        );
        Assert.False(info.IsLiquid);
    }

    [Fact]
    public void DerivePoolAccessInfo_DaysLockedMeta_ReturnsLockupDescription()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "someprotocol",
            poolMeta: "30 days lockup period"
        );

        Assert.NotNull(info.LockupDescription);
        Assert.Contains(
            expectedSubstring: "lockup",
            actualString: info.LockupDescription!,
            System.StringComparison.OrdinalIgnoreCase
        );
        Assert.False(info.IsLiquid);
    }

    [Fact]
    public void DerivePoolAccessInfo_KycFromBothMetaAndProject_KycRequired()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "maple",
            poolMeta: "KYC required for entry"
        );

        Assert.True(info.KycRequiredForEntry);
        Assert.True(info.KycRequiredForExit);
    }

    [Fact]
    public void DerivePoolAccessInfo_CanHaveBothKycAndLockup()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "goldfinch",
            poolMeta: "30 days lockup period"
        );

        Assert.True(info.KycRequiredForEntry);
        Assert.NotNull(info.LockupDescription);
        Assert.False(info.IsLiquid);
    }

    [Fact]
    public void DerivePoolAccessInfo_SwapExitWithLockup_BothSet()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "pendle",
            poolMeta: "Maturity 30 Jun 2026"
        );

        Assert.True(info.CanSwapToExit);
        Assert.NotNull(info.LockupDescription);
        Assert.False(info.IsLiquid);
    }
}
