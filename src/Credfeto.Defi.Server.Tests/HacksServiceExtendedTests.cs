using System.Collections.Generic;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class HacksServiceExtendedTests : TestBase
{
    [Fact]
    public void BuildHackMap_HackWithEmptyName_EmptySlugSkipped()
    {
        // ToSlug of an empty or whitespace name produces empty string
        // which triggers the AddToMap guard (string.IsNullOrEmpty(key))
        RawHack[] hacks =
        [
            new RawHack
            {
                Name = string.Empty,
                Date = 1000000,
                Amount = 1_000_000m,
                Classification = "Exploit",
                Technique = "Flash Loan",
                Source = "https://example.com",
                ParentProtocolId = null,
            },
        ];

        // Should not throw even if slug is empty
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(hacks);

        // No entries should be added since the name slug is empty
        Assert.Empty(map);
    }

    [Fact]
    public void BuildHackMap_HackWithParentProtocolId_IndexesByParentSlug()
    {
        RawHack[] hacks =
        [
            new RawHack
            {
                Name = "Some Protocol",
                Date = 2000000,
                Amount = 500_000m,
                Classification = "Bug",
                Technique = "Logic Error",
                Source = "https://example.com",
                ParentProtocolId = "parent#compound",
            },
        ];

        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(hacks);

        Assert.True(map.ContainsKey("compound"), userMessage: "Should index by parent slug 'compound'");
    }

    [Fact]
    public void BuildHackMap_HackWithParentProtocolBaseSlug_IndexesByBaseParentSlug()
    {
        RawHack[] hacks =
        [
            new RawHack
            {
                Name = "Aave Protocol",
                Date = 1500000,
                Amount = 750_000m,
                Classification = "Exploit",
                Technique = "Flash Loan",
                Source = "https://example.com",
                ParentProtocolId = "parent#compound-v3",
            },
        ];

        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(hacks);

        // compound-v3 base slug is "compound"
        Assert.True(map.ContainsKey("compound"), userMessage: "Should index by base parent slug 'compound'");
    }

    [Fact]
    public void BuildHackMap_MultipleHacks_AllIndexed()
    {
        RawHack[] hacks =
        [
            new RawHack
            {
                Name = "Alpha",
                Date = 1000,
                Amount = 100m,
                Classification = "A",
                Technique = "X",
                Source = "S",
                ParentProtocolId = null,
            },
            new RawHack
            {
                Name = "Beta",
                Date = 2000,
                Amount = 200m,
                Classification = "B",
                Technique = "Y",
                Source = "S",
                ParentProtocolId = null,
            },
        ];

        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(hacks);

        Assert.True(map.ContainsKey("alpha"), userMessage: "Should contain 'alpha' in map");
        Assert.True(map.ContainsKey("beta"), userMessage: "Should contain 'beta' in map");
    }

    [Fact]
    public void MatchHacks_PrefixMatch_ReturnsHacks()
    {
        // "compound" in the map, query "compound-v3" (starts with "compound-")
        RawHack[] hacks =
        [
            new RawHack
            {
                Name = "Compound",
                Date = 1000000,
                Amount = 1_000_000m,
                Classification = "Bug",
                Technique = "Logic",
                Source = "S",
                ParentProtocolId = null,
            },
        ];

        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(hacks);

        IReadOnlyList<HackInfo> result = HacksService.MatchHacks(projectSlug: "compound-v3", hackMap: map);

        Assert.NotEmpty(result);
    }

    [Fact]
    public void MatchHacks_EmptyMap_ReturnsEmpty()
    {
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap([]);

        IReadOnlyList<HackInfo> result = HacksService.MatchHacks(projectSlug: "aave", hackMap: map);

        Assert.Empty(result);
    }

    [Fact]
    public void MatchHacks_ResultsSortedByDateDescending()
    {
        // Two hacks under the same slug with different dates
        RawHack[] hacks =
        [
            new RawHack
            {
                Name = "Alpha Early",
                Date = 1000,
                Amount = 100m,
                Classification = "A",
                Technique = "X",
                Source = "S",
                ParentProtocolId = "parent#aave",
            },
            new RawHack
            {
                Name = "Alpha Late",
                Date = 9000,
                Amount = 200m,
                Classification = "A",
                Technique = "X",
                Source = "S",
                ParentProtocolId = "parent#aave",
            },
        ];

        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(hacks);

        IReadOnlyList<HackInfo> result = HacksService.MatchHacks(projectSlug: "aave", hackMap: map);

        Assert.True(result.Count >= 2, userMessage: "Should have at least two results");
        Assert.True(
            condition: result[0].Date >= result[1].Date,
            userMessage: "Results should be sorted by date descending"
        );
    }
}
