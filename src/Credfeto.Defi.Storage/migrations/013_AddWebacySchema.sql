IF
  NOT EXISTS (
    SELECT 1 FROM [sys].[schemas]
    WHERE [name] = N'Webacy'
  )
  BEGIN
    EXEC ('CREATE SCHEMA [Webacy]');
  END;
GO

IF OBJECT_ID(N'[Webacy].[TokenRisk]', N'U') IS NULL
  BEGIN
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
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'Webacy') AND [name] = N'TokenRiskRow'
  )
  BEGIN
    DROP TYPE [Webacy].[TokenRiskRow];
  END;
GO

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
GO

CREATE OR ALTER PROCEDURE [Webacy].[TokenRisk_Sync]
  @Rows [Webacy].[TokenRiskRow] READONLY
AS
BEGIN
  SET NOCOUNT ON;

  -- No WHEN NOT MATCHED BY SOURCE / DELETE branch: this is an on-demand per-key cache
  -- (SetWebacyAsync writes one contract at a time), not a bulk snapshot sync - see
  -- the TVP Sync Pattern exception in ai/local/database.instructions.md.
  MERGE [Webacy].[TokenRisk] AS Tgt
  USING @Rows AS Src ON Tgt.[Chain] = Src.[Chain] AND Tgt.[Address] = Src.[Address]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [RiskScore] = Src.[RiskScore],
        [RiskLevel] = Src.[RiskLevel],
        [IssueCount] = Src.[IssueCount],
        [HighRiskIssueCount] = Src.[HighRiskIssueCount],
        [MediumRiskIssueCount] = Src.[MediumRiskIssueCount],
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [Chain],
      [Address],
      [RiskScore],
      [RiskLevel],
      [IssueCount],
      [HighRiskIssueCount],
      [MediumRiskIssueCount],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Chain],
      Src.[Address],
      Src.[RiskScore],
      Src.[RiskLevel],
      Src.[IssueCount],
      Src.[HighRiskIssueCount],
      Src.[MediumRiskIssueCount],
      NULL,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    );
END;
GO

CREATE OR ALTER PROCEDURE [Webacy].[TokenRisk_GetByChainAndAddress]
  @Chain NVARCHAR(100),
  @Address NVARCHAR(100)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Chain],
    [Address],
    [RiskScore],
    [RiskLevel],
    [IssueCount],
    [HighRiskIssueCount],
    [MediumRiskIssueCount],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [Webacy].[TokenRisk]
  WHERE [Chain] = @Chain
    AND [Address] = @Address;
END;
GO
