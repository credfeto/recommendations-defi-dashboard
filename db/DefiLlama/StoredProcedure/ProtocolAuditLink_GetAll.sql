CREATE OR ALTER PROCEDURE [DefiLlama].[ProtocolAuditLink_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Slug],
    [AuditLink],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[ProtocolAuditLink]
  ORDER BY [Slug], [AuditLink];
END;
