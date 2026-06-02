using System.Collections.Generic;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class HacksServiceTests : TestBase
{
    private static readonly RawHack[] TestHacks =
    [
        new RawHack
        {
            Name = "Aave",
            Date = 1000000,
            Amount = 1_000_000m,
            Classification = "Exploit",
            Technique = "Flash Loan",
            Source = "https://example.com",
            ParentProtocolId = null,
        },
        new RawHack
        {
            Name = "Compound Finance",
            Date = 2000000,
            Amount = 500_000m,
            Classification = "Bug",
            Technique = "Logic Error",
            Source = "https://example.com",
            ParentProtocolId = "parent#compound",
        },
    ];

    [Fact]
    public void BuildHackMap_CreatesSlugKeyedEntries()
    {
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(TestHacks);

        Assert.True(map.ContainsKey("aave"), userMessage: "Hack map should contain 'aave' slug");
    }

    [Fact]
    public void MatchHacks_ExactSlugMatch_ReturnsHacks()
    {
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(TestHacks);
        IReadOnlyList<HackInfo> hacks = HacksService.MatchHacks(projectSlug: "aave", hackMap: map);

        Assert.NotEmpty(hacks);
    }

    [Fact]
    public void MatchHacks_VersionedSlugMatch_ReturnsBaseHacks()
    {
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(TestHacks);

        // "aave-v3" should match "aave" hack
        IReadOnlyList<HackInfo> hacks = HacksService.MatchHacks(projectSlug: "aave-v3", hackMap: map);

        Assert.NotEmpty(hacks);
    }

    [Fact]
    public void MatchHacks_NoMatch_ReturnsEmpty()
    {
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(TestHacks);
        IReadOnlyList<HackInfo> hacks = HacksService.MatchHacks(projectSlug: "unknown-protocol", hackMap: map);

        Assert.Empty(hacks);
    }

    [Fact]
    public void MatchHacks_ParentProtocolId_MatchesViaParent()
    {
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(TestHacks);
        IReadOnlyList<HackInfo> hacks = HacksService.MatchHacks(projectSlug: "compound", hackMap: map);

        Assert.NotEmpty(hacks);
    }

    [Fact]
    public void MatchHacks_DeduplicatesResults()
    {
        IReadOnlyDictionary<string, List<HackInfo>> map = HacksService.BuildHackMap(TestHacks);
        IReadOnlyList<HackInfo> hacks = HacksService.MatchHacks(projectSlug: "aave", hackMap: map);

        // Check no duplicates by name+date
        HashSet<string> keys = [];

        foreach (HackInfo hack in hacks)
        {
            string key = $"{hack.Name}|{hack.Date}";
            Assert.True(keys.Add(key), $"Duplicate hack found: {key}");
        }
    }
}
