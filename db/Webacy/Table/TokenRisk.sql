CREATE TABLE [Webacy].[TokenRisk]
(
  [Chain] NVARCHAR(100) NOT NULL,
  [Address] NVARCHAR(100) NOT NULL,
  [RiskScore] FLOAT(53) NULL,
  [RiskLevel] NVARCHAR(20) NULL,
  [IssueCount] INT NULL,
  [HighRiskIssueCount] INT NULL,
  [MediumRiskIssueCount] INT NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  [DataDate] DATETIMEOFFSET NULL,
  CONSTRAINT [PK_Webacy_TokenRisk] PRIMARY KEY ([Chain], [Address])
);
