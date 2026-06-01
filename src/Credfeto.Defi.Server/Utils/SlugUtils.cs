using System.Text.RegularExpressions;

namespace Credfeto.Defi.Server.Utils;

/// <summary>
///     URL slug normalisation utilities.
/// </summary>
internal static partial class SlugUtils
{
    [GeneratedRegex(pattern: "[^a-z0-9]+", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex NonAlphanumericRegex { get; }

    [GeneratedRegex(pattern: "(^-|-$)", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex LeadingTrailingDashRegex { get; }

    [GeneratedRegex(pattern: "-v\\d+.*$", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex VersionSuffixRegex { get; }

    /// <summary>
    ///     Normalises a display name or path segment into a URL-style slug.
    /// </summary>
    public static string ToSlug(string str)
    {
        string lower = str.ToLowerInvariant();
        string nonAlphanumericReplaced = NonAlphanumericRegex.Replace(input: lower, replacement: "-");

        return LeadingTrailingDashRegex.Replace(input: nonAlphanumericReplaced, replacement: string.Empty);
    }

    /// <summary>
    ///     Strips common version suffixes so "aave-v3" base-matches "aave".
    /// </summary>
    public static string BaseSlug(string slug)
    {
        return VersionSuffixRegex.Replace(input: slug, replacement: string.Empty);
    }
}
