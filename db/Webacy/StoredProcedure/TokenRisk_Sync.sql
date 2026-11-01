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
