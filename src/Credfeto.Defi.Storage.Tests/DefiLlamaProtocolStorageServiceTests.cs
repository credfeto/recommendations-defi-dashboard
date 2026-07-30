using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class DefiLlamaProtocolStorageServiceTests : TestBase
{
    private static readonly DateTimeOffset FixedNow = MockDateTimeSources.Past.GetUtcNow();

    private readonly FakeDatabase _database;
    private readonly DefiLlamaProtocolStorageService _storage;

    public DefiLlamaProtocolStorageServiceTests()
    {
        this._database = new FakeDatabase();
        this._storage = new DefiLlamaProtocolStorageService(database: this._database);
    }

    [Fact]
    public async Task GetAllProtocolsAsync_NoProtocols_ReturnsEmptyListAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolRow>>([]);
        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolAuditLinkRow>>([]);

        IReadOnlyList<RawProtocol> result = await this._storage.GetAllProtocolsAsync(cancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllProtocolsAsync_ProtocolWithNoAuditLinks_ReturnsNullAuditLinksAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        DefiLlamaProtocolRow row = new(
            Slug: "aave-v3",
            Audits: "2",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolRow>>([row]);
        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolAuditLinkRow>>([]);

        IReadOnlyList<RawProtocol> result = await this._storage.GetAllProtocolsAsync(cancellationToken);

        Assert.Single(result);
        Assert.Equal(expected: "aave-v3", actual: result[0].Slug);
        Assert.Equal(expected: "2", actual: result[0].Audits);
        Assert.Null(result[0].AuditLinks);
    }

    [Fact]
    public async Task GetAllProtocolsAsync_ProtocolWithMultipleAuditLinks_MapsAllLinksAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        DefiLlamaProtocolRow protocolRow = new(
            Slug: "aave-v3",
            Audits: "2",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        DefiLlamaProtocolAuditLinkRow link1 = new(
            Slug: "aave-v3",
            AuditLink: "https://example.com/audit1",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );
        DefiLlamaProtocolAuditLinkRow link2 = new(
            Slug: "aave-v3",
            AuditLink: "https://example.com/audit2",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolRow>>([protocolRow]);
        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolAuditLinkRow>>([link1, link2]);

        IReadOnlyList<RawProtocol> result = await this._storage.GetAllProtocolsAsync(cancellationToken);

        Assert.Single(result);
        Assert.NotNull(result[0].AuditLinks);
        Assert.Equal(expected: 2, actual: result[0].AuditLinks!.Length);
        Assert.Contains(link1.AuditLink, result[0].AuditLinks!);
        Assert.Contains(link2.AuditLink, result[0].AuditLinks!);
    }

    [Fact]
    public async Task GetAllProtocolsAsync_MultipleProtocols_AuditLinksGroupedByCorrectSlugAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        DefiLlamaProtocolRow aaveRow = new(
            Slug: "aave-v3",
            Audits: "2",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );
        DefiLlamaProtocolRow compoundRow = new(
            Slug: "compound-v3",
            Audits: null,
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        DefiLlamaProtocolAuditLinkRow aaveLink = new(
            Slug: "aave-v3",
            AuditLink: "https://example.com/aave-audit",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolRow>>([aaveRow, compoundRow]);
        this._database.WithReturn<IReadOnlyList<DefiLlamaProtocolAuditLinkRow>>([aaveLink]);

        IReadOnlyList<RawProtocol> result = await this._storage.GetAllProtocolsAsync(cancellationToken);

        Assert.Equal(expected: 2, actual: result.Count);

        RawProtocol aave = result[0];
        RawProtocol compound = result[1];

        Assert.NotNull(aave.AuditLinks);
        Assert.Single(aave.AuditLinks!);
        Assert.Equal(expected: aaveLink.AuditLink, actual: aave.AuditLinks![0]);

        Assert.Null(compound.AuditLinks);
    }

    [Fact]
    public async Task StoreProtocolsAsync_ProtocolsWithAndWithoutAuditLinks_CompletesWithoutThrowingAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IReadOnlyList<RawProtocol> protocols =
        [
            new RawProtocol
            {
                Slug = "aave-v3",
                Audits = "2",
                AuditLinks = ["https://example.com/audit1", "https://example.com/audit1"],
            },
            new RawProtocol
            {
                Slug = "compound-v3",
                Audits = null,
                AuditLinks = null,
            },
        ];

        await this._storage.StoreProtocolsAsync(
            protocols: protocols,
            dataDate: FixedNow,
            cancellationToken: cancellationToken
        );
    }
}
