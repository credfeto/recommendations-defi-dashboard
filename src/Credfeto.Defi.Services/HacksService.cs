using System;
using System.Collections.Generic;
using System.Linq;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services.Utils;

namespace Credfeto.Defi.Services;

public static class HacksService
{
    public static IReadOnlyDictionary<string, List<HackInfo>> BuildHackMap(IReadOnlyList<RawHack> hacks)
    {
        Dictionary<string, List<HackInfo>> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (RawHack h in hacks)
        {
            HackInfo info = new()
            {
                Name = h.Name,
                Date = h.Date,
                AmountUsd = h.Amount,
                Classification = h.Classification ?? "Unknown",
                Technique = h.Technique ?? "Unknown",
                Source = h.Source,
            };

            string nameSlug = SlugUtils.ToSlug(h.Name);
            AddToMap(map: map, key: nameSlug, info: info);
            AddToMap(map: map, key: SlugUtils.BaseSlug(nameSlug), info: info);

            if (!string.IsNullOrEmpty(h.ParentProtocolId))
            {
                string parentSlug = h.ParentProtocolId.Replace(
                    oldValue: "parent#",
                    newValue: string.Empty,
                    comparisonType: StringComparison.Ordinal
                );
                AddToMap(map: map, key: parentSlug, info: info);
                AddToMap(map: map, key: SlugUtils.BaseSlug(parentSlug), info: info);
            }
        }

        return map;
    }

    public static IReadOnlyList<HackInfo> MatchHacks(
        string projectSlug,
        IReadOnlyDictionary<string, List<HackInfo>> hackMap
    )
    {
        Dictionary<string, HackInfo> seen = new(StringComparer.Ordinal);

        Collect(key: projectSlug, hackMap: hackMap, seen: seen);
        Collect(key: SlugUtils.BaseSlug(projectSlug), hackMap: hackMap, seen: seen);

        // Also match any hack key that is a prefix of this project slug
        // e.g. "compound-v3" matches hack key "compound"
        foreach (
            KeyValuePair<string, List<HackInfo>> entry in hackMap.Where(e =>
                string.Equals(a: projectSlug, b: e.Key, comparisonType: StringComparison.OrdinalIgnoreCase)
                || projectSlug.StartsWith(value: e.Key + "-", comparisonType: StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            foreach (HackInfo hack in entry.Value)
            {
                string seenKey = $"{hack.Name}|{hack.Date}";
                _ = seen.TryAdd(key: seenKey, value: hack);
            }
        }

        List<HackInfo> result = [.. seen.Values];
        result.Sort((a, b) => b.Date.CompareTo(a.Date));

        return result;
    }

    private static void AddToMap(Dictionary<string, List<HackInfo>> map, string key, HackInfo info)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!map.TryGetValue(key: key, out List<HackInfo>? list))
        {
            list = [];
            map[key] = list;
        }

        list.Add(info);
    }

    private static void Collect(
        string key,
        IReadOnlyDictionary<string, List<HackInfo>> hackMap,
        Dictionary<string, HackInfo> seen
    )
    {
        if (!hackMap.TryGetValue(key: key, out List<HackInfo>? items))
        {
            return;
        }

        foreach (HackInfo h in items)
        {
            string seenKey = $"{h.Name}|{h.Date}";
            _ = seen.TryAdd(key: seenKey, value: h);
        }
    }
}
