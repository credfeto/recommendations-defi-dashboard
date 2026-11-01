using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Chain}/{Address} riskScore={RiskScore}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in ContractSecurityCacheService"
)]
internal sealed record WebacyTokenRiskSyncRow(
    string Chain,
    string Address,
    double? RiskScore,
    string? RiskLevel,
    int? IssueCount,
    int? HighRiskIssueCount,
    int? MediumRiskIssueCount
);
