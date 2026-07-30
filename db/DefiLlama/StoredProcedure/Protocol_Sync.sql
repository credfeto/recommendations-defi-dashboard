CREATE OR ALTER PROCEDURE [DefiLlama].[Protocol_Sync]
  @Protocols [DefiLlama].[ProtocolRow] READONLY,
  @AuditLinks [DefiLlama].[ProtocolAuditLinkRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[Protocol] AS Tgt
  USING @Protocols AS Src ON Tgt.[Slug] = Src.[Slug]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Audits] = Src.[Audits],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Slug], [Audits], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Slug], Src.[Audits], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;

  MERGE [DefiLlama].[ProtocolAuditLink] AS Tgt
  USING @AuditLinks AS Src ON Tgt.[Slug] = Src.[Slug] AND Tgt.[AuditLink] = Src.[AuditLink]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Slug], [AuditLink], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Slug], Src.[AuditLink], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
