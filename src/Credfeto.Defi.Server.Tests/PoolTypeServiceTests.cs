using System;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PoolTypeServiceTests : TestBase
{
    [Fact]
    public void GetAllPoolTypes_ReturnsExactlyFiveRecords()
    {
        PoolTypeMetadata[] types = PoolTypeService.GetAllPoolTypes();
        Assert.Equal(expected: 5, actual: types.Length);
    }

    [Fact]
    public void GetAllPoolTypes_ContainsEth()
    {
        PoolTypeMetadata[] types = PoolTypeService.GetAllPoolTypes();
        Assert.Contains(types, t => string.Equals(a: t.Id, b: "ETH", comparisonType: StringComparison.Ordinal));
    }

    [Fact]
    public void GetAllPoolTypes_ContainsStables()
    {
        PoolTypeMetadata[] types = PoolTypeService.GetAllPoolTypes();
        Assert.Contains(types, t => string.Equals(a: t.Id, b: "STABLES", comparisonType: StringComparison.Ordinal));
    }

    [Fact]
    public void GetAllPoolTypes_ContainsHighYield()
    {
        PoolTypeMetadata[] types = PoolTypeService.GetAllPoolTypes();
        Assert.Contains(types, t => string.Equals(a: t.Id, b: "HIGH_YIELD", comparisonType: StringComparison.Ordinal));
    }

    [Fact]
    public void GetAllPoolTypes_ContainsLowTvl()
    {
        PoolTypeMetadata[] types = PoolTypeService.GetAllPoolTypes();
        Assert.Contains(types, t => string.Equals(a: t.Id, b: "LOW_TVL", comparisonType: StringComparison.Ordinal));
    }

    [Fact]
    public void GetAllPoolTypes_ContainsBlueChip()
    {
        PoolTypeMetadata[] types = PoolTypeService.GetAllPoolTypes();
        Assert.Contains(types, t => string.Equals(a: t.Id, b: "BLUE_CHIP", comparisonType: StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ETH")]
    [InlineData("STABLES")]
    [InlineData("HIGH_YIELD")]
    [InlineData("LOW_TVL")]
    [InlineData("BLUE_CHIP")]
    [InlineData("eth")]
    [InlineData("stables")]
    [InlineData("high_yield")]
    [InlineData("low_tvl")]
    [InlineData("blue_chip")]
    public void IsValidPoolType_ValidId_ReturnsTrue(string poolTypeId)
    {
        bool result = PoolTypeService.IsValidPoolType(poolTypeId);
        Assert.True(result, userMessage: $"Expected '{poolTypeId}' to be a valid pool type");
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("UNKNOWN")]
    [InlineData("BTC")]
    public void IsValidPoolType_UnknownId_ReturnsFalse(string poolTypeId)
    {
        bool result = PoolTypeService.IsValidPoolType(poolTypeId);
        Assert.False(result, userMessage: $"Expected '{poolTypeId}' to be an invalid pool type");
    }

    [Fact]
    public void IsValidPoolType_EmptyString_ReturnsFalse()
    {
        bool result = PoolTypeService.IsValidPoolType(string.Empty);
        Assert.False(result, userMessage: "Expected empty string to be an invalid pool type");
    }
}
