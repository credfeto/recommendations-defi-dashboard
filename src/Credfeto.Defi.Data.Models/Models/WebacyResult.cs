using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("riskScore={RiskScore} riskLevel={RiskLevel} issues={IssueCount}")]
public sealed record WebacyResult
{
    public double? RiskScore { get; init; }

    public string? RiskLevel { get; init; }

    public int? IssueCount { get; init; }

    public int? HighRiskIssueCount { get; init; }

    public int? MediumRiskIssueCount { get; init; }
}
