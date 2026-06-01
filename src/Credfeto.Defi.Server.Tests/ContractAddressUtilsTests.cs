using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Utils;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ContractAddressUtilsTests : TestBase
{
    [Fact]
    public void IsContractAddress_Valid0xAddress_ReturnsTrue()
    {
        bool result = ContractAddressUtils.IsContractAddress("0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48");
        Assert.True(result, userMessage: "A valid 42-char 0x address should be recognised as a contract address");
    }

    [Fact]
    public void IsContractAddress_Non0xString_ReturnsFalse()
    {
        bool result = ContractAddressUtils.IsContractAddress("USDC");
        Assert.False(result, userMessage: "A non-0x string should not be recognised as a contract address");
    }

    [Fact]
    public void IsContractAddress_WrongLength_ReturnsFalse()
    {
        // 0x + 38 hex chars (too short)
        bool result = ContractAddressUtils.IsContractAddress("0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606e");
        Assert.False(result, userMessage: "A short address should not be recognised as a contract address");
    }

    [Fact]
    public void IsContractAddress_TooLong_ReturnsFalse()
    {
        // 0x + 42 hex chars (too long)
        bool result = ContractAddressUtils.IsContractAddress("0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB4800");
        Assert.False(result, userMessage: "A too-long address should not be recognised as a contract address");
    }

    [Fact]
    public void IsContractAddress_EmptyString_ReturnsFalse()
    {
        bool result = ContractAddressUtils.IsContractAddress(string.Empty);
        Assert.False(result, userMessage: "An empty string should not be recognised as a contract address");
    }

    [Fact]
    public void BuildContractAddresses_PoolWithUnderlyingTokens_ReturnsLowercased()
    {
        RawPool pool = new()
        {
            Project = "aave",
            Chain = "Ethereum",
            Symbol = "USDC",
            TvlUsd = 1_000_000,
            Apy = 5.0,
            Stablecoin = true,
            IlRisk = "no",
            PoolId = "uuid-not-a-contract",
            Predictions = new RawPredictions(),
            UnderlyingTokens = ["0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48"],
        };

        string[] addresses = ContractAddressUtils.BuildContractAddresses(pool);

        Assert.Single(addresses);
        Assert.Equal(expected: "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", actual: addresses[0]);
    }

    [Fact]
    public void BuildContractAddresses_PoolWithRewardTokens_ReturnsRewardAddresses()
    {
        RawPool pool = new()
        {
            Project = "aave",
            Chain = "Ethereum",
            Symbol = "USDC",
            TvlUsd = 1_000_000,
            Apy = 5.0,
            Stablecoin = false,
            IlRisk = "no",
            PoolId = "uuid-not-a-contract",
            Predictions = new RawPredictions(),
            RewardTokens = ["0xdAC17F958D2ee523a2206206994597C13D831ec7"],
        };

        string[] addresses = ContractAddressUtils.BuildContractAddresses(pool);

        Assert.Single(addresses);
        Assert.Equal(expected: "0xdac17f958d2ee523a2206206994597c13d831ec7", actual: addresses[0]);
    }

    [Fact]
    public void BuildContractAddresses_PoolIdIsContractAddress_IncludesPoolId()
    {
        RawPool pool = new()
        {
            Project = "pendle",
            Chain = "Ethereum",
            Symbol = "PT-USDC",
            TvlUsd = 5_000_000,
            Apy = 8.0,
            Stablecoin = false,
            IlRisk = "no",
            PoolId = "0xC374f7eC85F8C7DE3207a10bB1978bA104bdA3B2",
            Predictions = new RawPredictions(),
        };

        string[] addresses = ContractAddressUtils.BuildContractAddresses(pool);

        Assert.Single(addresses);
        Assert.Equal(expected: "0xc374f7ec85f8c7de3207a10bb1978ba104bda3b2", actual: addresses[0]);
    }

    [Fact]
    public void BuildContractAddresses_PoolIdIsUuid_NotIncluded()
    {
        RawPool pool = new()
        {
            Project = "aave",
            Chain = "Ethereum",
            Symbol = "USDC",
            TvlUsd = 1_000_000,
            Apy = 5.0,
            Stablecoin = true,
            IlRisk = "no",
            PoolId = "abc123-uuid-no-contract",
            Predictions = new RawPredictions(),
        };

        string[] addresses = ContractAddressUtils.BuildContractAddresses(pool);

        Assert.Empty(addresses);
    }

    [Fact]
    public void BuildContractAddresses_UnderlyingNonContractString_Filtered()
    {
        RawPool pool = new()
        {
            Project = "aave",
            Chain = "Ethereum",
            Symbol = "USDC",
            TvlUsd = 1_000_000,
            Apy = 5.0,
            Stablecoin = true,
            IlRisk = "no",
            PoolId = "uuid-only",
            Predictions = new RawPredictions(),
            UnderlyingTokens = ["USDC", "not-an-address"],
        };

        string[] addresses = ContractAddressUtils.BuildContractAddresses(pool);

        Assert.Empty(addresses);
    }

    [Fact]
    public void BuildContractAddresses_DuplicateAddresses_Deduplicated()
    {
        const string ADDR = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

        RawPool pool = new()
        {
            Project = "aave",
            Chain = "Ethereum",
            Symbol = "USDC",
            TvlUsd = 1_000_000,
            Apy = 5.0,
            Stablecoin = true,
            IlRisk = "no",
            PoolId = ADDR,
            Predictions = new RawPredictions(),
            UnderlyingTokens = [ADDR],
            RewardTokens = [ADDR],
        };

        string[] addresses = ContractAddressUtils.BuildContractAddresses(pool);

        Assert.Single(addresses);
    }

    [Fact]
    public void BuildContractAddresses_CombinationOfSources_IncludesAll()
    {
        const string UNDERLYING = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
        const string REWARD = "0xdAC17F958D2ee523a2206206994597C13D831ec7";
        const string POOL_ID = "0xC374f7eC85F8C7DE3207a10bB1978bA104bdA3B2";

        RawPool pool = new()
        {
            Project = "pendle",
            Chain = "Ethereum",
            Symbol = "PT-USDC",
            TvlUsd = 5_000_000,
            Apy = 8.0,
            Stablecoin = false,
            IlRisk = "no",
            PoolId = POOL_ID,
            Predictions = new RawPredictions(),
            UnderlyingTokens = [UNDERLYING],
            RewardTokens = [REWARD],
        };

        string[] addresses = ContractAddressUtils.BuildContractAddresses(pool);

        Assert.Equal(expected: 3, actual: addresses.Length);
    }
}
