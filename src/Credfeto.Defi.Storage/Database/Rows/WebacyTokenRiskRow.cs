using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Chain}/{Address} riskScore={RiskScore}")]
public sealed record WebacyTokenRiskRow(
    string Chain,
    string Address,
    double? RiskScore,
    string? RiskLevel,
    int? IssueCount,
    int? HighRiskIssueCount,
    int? MediumRiskIssueCount,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
