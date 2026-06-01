using System.Collections.Generic;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ProtocolsServiceTests : TestBase
{
    [Fact]
    public void BuildProtocolAuditMap_EmptyList_ReturnsEmptyMap()
    {
        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap([]);
        Assert.Empty(map);
    }

    [Fact]
    public void BuildProtocolAuditMap_ProtocolWithEmptySlug_IsSkipped()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = string.Empty,
                Audits = "2",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);
        Assert.Empty(map);
    }

    [Fact]
    public void BuildProtocolAuditMap_ProtocolWithNullSlug_IsSkipped()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = null!,
                Audits = "2",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);
        Assert.Empty(map);
    }

    [Fact]
    public void BuildProtocolAuditMap_ProtocolWithSlug_IsIndexedBySlug()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = "aave-v3",
                Audits = "3",
                AuditLinks = ["https://audit.example.com"],
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);

        Assert.True(map.ContainsKey("aave-v3"), userMessage: "Should contain exact slug");
        Assert.Equal(expected: 3, actual: map["aave-v3"].Audits);
    }

    [Fact]
    public void BuildProtocolAuditMap_VersionedSlug_AlsoIndexedByBaseSlug()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = "aave-v3",
                Audits = "1",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);

        Assert.True(map.ContainsKey("aave"), userMessage: "Should also index by base slug 'aave'");
    }

    [Fact]
    public void BuildProtocolAuditMap_BaseSlugProtocol_NotDuplicatedAsBaseSlug()
    {
        // "aave" is already the base slug - should only appear once
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = "aave",
                Audits = "2",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);

        Assert.True(map.ContainsKey("aave"), userMessage: "Should contain 'aave' in map");
        Assert.Single(map);
    }

    [Fact]
    public void BuildProtocolAuditMap_DuplicateSlugs_FirstOneWins()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = "aave",
                Audits = "3",
                AuditLinks = null,
            },
            new RawProtocol
            {
                Slug = "aave",
                Audits = "5",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);

        Assert.True(map.ContainsKey("aave"), userMessage: "Should contain 'aave' in map after dedup");
        Assert.Equal(expected: 3, actual: map["aave"].Audits);
    }

    [Fact]
    public void BuildProtocolAuditMap_NullAuditLinks_ReturnsEmptyArray()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = "aave",
                Audits = "1",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);

        Assert.NotNull(map["aave"].AuditLinks);
        Assert.Empty(map["aave"].AuditLinks);
    }

    [Fact]
    public void BuildProtocolAuditMap_InvalidAuditCount_DefaultsToZero()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = "myprotocol",
                Audits = "not-a-number",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);

        Assert.Equal(expected: 0, actual: map["myprotocol"].Audits);
    }

    [Fact]
    public void MatchAuditInfo_ExactSlugMatch_ReturnsInfo()
    {
        RawProtocol[] protocols =
        [
            new RawProtocol
            {
                Slug = "aave-v3",
                Audits = "2",
                AuditLinks = null,
            },
        ];

        IReadOnlyDictionary<string, AuditInfo> map = ProtocolsService.BuildProtocolAuditMap(protocols);
        AuditInfo? result = ProtocolsService.MatchAuditInfo(projectSlug: "aave-v3", protocolMap: map);

        Assert.NotNull(result);
        Assert.Equal(expected: 2, actual: result.Audits);
    }

    [Fact]
    public void MatchAuditInfo_BaseSlugFallback_ReturnsInfo()
    {
        // Map only has "aave" (base slug), but we query for "aave-v3"
        Dictionary<string, AuditInfo> map = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["aave"] = new AuditInfo { Audits = 4, AuditLinks = [] },
        };

        AuditInfo? result = ProtocolsService.MatchAuditInfo(projectSlug: "aave-v3", protocolMap: map);

        Assert.NotNull(result);
        Assert.Equal(expected: 4, actual: result.Audits);
    }

    [Fact]
    public void MatchAuditInfo_NoMatch_ReturnsNull()
    {
        Dictionary<string, AuditInfo> map = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["compound"] = new AuditInfo { Audits = 1, AuditLinks = [] },
        };

        AuditInfo? result = ProtocolsService.MatchAuditInfo(projectSlug: "unknown-protocol", protocolMap: map);

        Assert.Null(result);
    }

    [Fact]
    public void MatchAuditInfo_BaseSlugSameAsSlug_DoesNotDoubleLookup()
    {
        // When base slug equals project slug (no version suffix), still returns null when not found
        Dictionary<string, AuditInfo> map = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["something-else"] = new AuditInfo { Audits = 1, AuditLinks = [] },
        };

        AuditInfo? result = ProtocolsService.MatchAuditInfo(projectSlug: "aave", protocolMap: map);

        Assert.Null(result);
    }
}
