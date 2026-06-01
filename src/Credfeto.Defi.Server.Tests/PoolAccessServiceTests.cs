using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PoolAccessServiceTests : TestBase
{
    [Fact]
    public void DerivePoolAccessInfo_NoMeta_ReturnsNullKyc()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(project: "aave", poolMeta: null);

        Assert.Null(info.KycRequiredForEntry);
        Assert.Null(info.KycRequiredForExit);
    }

    [Theory]
    [InlineData("institutional investors only")]
    [InlineData("accredited investors")]
    [InlineData("KYC required")]
    [InlineData("whitelist only")]
    [InlineData("qualified purchasers")]
    [InlineData("permissioned pool")]
    public void DerivePoolAccessInfo_KycMeta_ReturnsKycRequired(string meta)
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(project: "someprotocol", poolMeta: meta);

        Assert.True(info.KycRequiredForEntry);
        Assert.True(info.KycRequiredForExit);
    }

    [Theory]
    [InlineData("maple")]
    [InlineData("maple-v2")]
    [InlineData("centrifuge")]
    [InlineData("credix")]
    [InlineData("goldfinch")]
    public void DerivePoolAccessInfo_KycProject_ReturnsKycRequired(string project)
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(project: project, poolMeta: null);

        Assert.True(info.KycRequiredForEntry);
    }

    [Theory]
    [InlineData("uniswap-v3")]
    [InlineData("curve")]
    [InlineData("balancer")]
    [InlineData("pendle")]
    public void DerivePoolAccessInfo_SwapExitProject_ReturnsCanSwapToExit(string project)
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(project: project, poolMeta: null);

        Assert.True(info.CanSwapToExit);
    }

    [Fact]
    public void DerivePoolAccessInfo_NonSwapProject_ReturnsNullCanSwap()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(project: "lending-protocol", poolMeta: null);

        Assert.Null(info.CanSwapToExit);
    }

    [Fact]
    public void DerivePoolAccessInfo_LockupMeta_ReturnsLockupDescription()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "someprotocol",
            poolMeta: "14 days unstaking period"
        );

        Assert.NotNull(info.LockupDescription);
        Assert.False(info.IsLiquid);
    }

    [Fact]
    public void DerivePoolAccessInfo_MaturityMeta_ReturnsFixedTermLockup()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(
            project: "pendle",
            poolMeta: "Maturity 30 Jun 2025"
        );

        Assert.NotNull(info.LockupDescription);
        Assert.Contains(
            expectedSubstring: "maturity",
            actualString: info.LockupDescription!,
            System.StringComparison.OrdinalIgnoreCase
        );
        Assert.False(info.IsLiquid);
    }

    [Fact]
    public void DerivePoolAccessInfo_NoLockup_ReturnsNullIsLiquid()
    {
        PoolAccessInfo info = PoolAccessService.DerivePoolAccessInfo(project: "aave", poolMeta: "normal pool");

        Assert.Null(info.IsLiquid);
        Assert.Null(info.LockupDescription);
    }
}
