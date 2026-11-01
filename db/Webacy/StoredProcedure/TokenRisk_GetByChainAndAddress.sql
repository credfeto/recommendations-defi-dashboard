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
