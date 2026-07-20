using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Storage;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ContractSecurityCacheServiceTests : TestBase
{
    private static readonly DateTimeOffset FixedNow = new(year: 2024, month: 6, day: 1, hour: 12, minute: 0, second: 0, offset: TimeSpan.Zero);

    private static readonly ContractSecurityRow SampleRow = new(
        Chain: "Ethereum",
        Address: "0xabc123def456abc123def456abc123def456abc1",
        ParentAddress: null,
        IsOpenSource: true,
        IsHoneypot: false,
        IsProxy: false,
        BuyTax: 0.0,
        SellTax: 0.0,
        TransferTax: 0.0,
        CannotBuy: false,
        HoneypotWithSameCreator: false,
        TokenName: "TestToken",
        TokenSymbol: "TEST",
        CheckedAt: FixedNow - TimeSpan.FromHours(1)
    );

    private readonly FakeDatabase _database;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ContractSecurityCacheService _cache;

    public ContractSecurityCacheServiceTests()
    {
        this._timeProvider = new FakeTimeProvider(startDateTime: FixedNow);
        this._database = new FakeDatabase();
        this._cache = new ContractSecurityCacheService(database: this._database, timeProvider: this._timeProvider);
    }

    [Fact]
    public async Task GetAsync_CacheMiss_ReturnsNullAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        ContractSecurityInfo? result = await this._cache.GetAsync(
            chain: "Ethereum",
            address: "0xdeadbeef0000000000000000000000000000dead",
            cancellationToken: cancellationToken
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_FreshEntry_ReturnsMappedInfoAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<ContractSecurityRow?>(SampleRow);

        ContractSecurityInfo? result = await this._cache.GetAsync(
            chain: SampleRow.Chain,
            address: SampleRow.Address,
            cancellationToken: cancellationToken
        );

        Assert.NotNull(result);
        Assert.Equal(expected: SampleRow.Chain, actual: result.Chain);
        Assert.Equal(expected: SampleRow.Address, actual: result.Address);
        Assert.Equal(expected: SampleRow.TokenName, actual: result.TokenName);
        Assert.True(result.IsOpenSource);
        Assert.False(result.IsHoneypot);
    }

    [Fact]
    public async Task GetAsync_ExpiredEntry_ReturnsNullAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        ContractSecurityRow expiredRow = SampleRow with { CheckedAt = FixedNow - TimeSpan.FromHours(25) };
        this._database.WithReturn<ContractSecurityRow?>(expiredRow);

        ContractSecurityInfo? result = await this._cache.GetAsync(
            chain: SampleRow.Chain,
            address: SampleRow.Address,
            cancellationToken: cancellationToken
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task GetChildrenAsync_NoChildren_ReturnsEmptyListAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<IReadOnlyList<ContractSecurityRow>>([]);

        IReadOnlyList<ContractSecurityInfo> children = await this._cache.GetChildrenAsync(
            chain: "Ethereum",
            parentAddress: "0xdeadbeef0000000000000000000000000000dead",
            cancellationToken: cancellationToken
        );

        Assert.Empty(children);
    }

    [Fact]
    public async Task GetChildrenAsync_WithChildren_ReturnsChildrenAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        const string PARENT_ADDRESS = "0xparent00000000000000000000000000000000ab";

        ContractSecurityRow childRow = new(
            Chain: "Ethereum",
            Address: "0xchild000000000000000000000000000000000ab",
            ParentAddress: PARENT_ADDRESS,
            IsOpenSource: null,
            IsHoneypot: null,
            IsProxy: false,
            BuyTax: null,
            SellTax: null,
            TransferTax: null,
            CannotBuy: null,
            HoneypotWithSameCreator: null,
            TokenName: null,
            TokenSymbol: null,
            CheckedAt: FixedNow - TimeSpan.FromHours(1)
        );

        this._database.WithReturn<IReadOnlyList<ContractSecurityRow>>([childRow]);

        IReadOnlyList<ContractSecurityInfo> children = await this._cache.GetChildrenAsync(
            chain: "Ethereum",
            parentAddress: PARENT_ADDRESS,
            cancellationToken: cancellationToken
        );

        Assert.Single(children);
    }

    [Fact]
    public async Task SetAsync_CallsUpsertAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        ContractSecurityInfo info = new()
        {
            Chain = "Arbitrum",
            Address = "0xnullable0000000000000000000000000000001a",
        };

        await this._cache.SetAsync(info: info, cancellationToken: cancellationToken);
    }
}
