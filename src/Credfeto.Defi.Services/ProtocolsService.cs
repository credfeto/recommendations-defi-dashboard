using System;
using System.Collections.Generic;
using System.Globalization;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services.Utils;

namespace Credfeto.Defi.Services;

/// <summary>
///     Builds and queries a slug-keyed map of protocol audit information.
/// </summary>
public static class ProtocolsService
{
    /// <summary>
    ///     Builds a map from protocol slug to audit information.
    /// </summary>
    public static IReadOnlyDictionary<string, AuditInfo> BuildProtocolAuditMap(IReadOnlyList<RawProtocol> protocols)
    {
        Dictionary<string, AuditInfo> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (RawProtocol p in protocols)
        {
            if (string.IsNullOrEmpty(p.Slug))
            {
                continue;
            }

            AuditInfo info = new()
            {
                Audits = int.TryParse(
                    s: p.Audits,
                    style: NumberStyles.Integer,
                    provider: CultureInfo.InvariantCulture,
                    result: out int count
                )
                    ? count
                    : 0,
                AuditLinks = p.AuditLinks ?? [],
            };

            _ = map.TryAdd(key: p.Slug, value: info);

            string baseSlug = SlugUtils.BaseSlug(p.Slug);

            if (!string.Equals(a: baseSlug, b: p.Slug, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                _ = map.TryAdd(key: baseSlug, value: info);
            }
        }

        return map;
    }

    /// <summary>
    ///     Returns audit info for a given project slug, or null if not found.
    /// </summary>
    public static AuditInfo? MatchAuditInfo(string projectSlug, IReadOnlyDictionary<string, AuditInfo> protocolMap)
    {
        if (protocolMap.TryGetValue(key: projectSlug, out AuditInfo? info))
        {
            return info;
        }

        string baseSlug = SlugUtils.BaseSlug(projectSlug);

        return
            !string.Equals(a: baseSlug, b: projectSlug, comparisonType: StringComparison.OrdinalIgnoreCase)
            && protocolMap.TryGetValue(key: baseSlug, out AuditInfo? baseInfo)
            ? baseInfo
            : null;
    }
}
