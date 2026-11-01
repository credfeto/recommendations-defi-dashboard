CREATE TYPE [Webacy].[TokenRiskRow] AS TABLE
(
  [Chain] NVARCHAR(100) NOT NULL,
  [Address] NVARCHAR(100) NOT NULL,
  [RiskScore] FLOAT(53) NULL,
  [RiskLevel] NVARCHAR(20) NULL,
  [IssueCount] INT NULL,
  [HighRiskIssueCount] INT NULL,
  [MediumRiskIssueCount] INT NULL
);
