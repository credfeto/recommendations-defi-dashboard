using System.Text.RegularExpressions;

namespace Credfeto.Defi.Services.Utils;

public static partial class SlugUtils
{
    [GeneratedRegex(pattern: "[^a-z0-9]+", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex NonAlphanumericRegex { get; }

    [GeneratedRegex(pattern: "(^-|-$)", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex LeadingTrailingDashRegex { get; }

    [GeneratedRegex(pattern: "-v\\d+.*$", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex VersionSuffixRegex { get; }

    public static string ToSlug(string str)
    {
        string lower = str.ToLowerInvariant();
        string nonAlphanumericReplaced = NonAlphanumericRegex.Replace(input: lower, replacement: "-");

        return LeadingTrailingDashRegex.Replace(input: nonAlphanumericReplaced, replacement: string.Empty);
    }

    public static string BaseSlug(string slug)
    {
        return VersionSuffixRegex.Replace(input: slug, replacement: string.Empty);
    }
}
