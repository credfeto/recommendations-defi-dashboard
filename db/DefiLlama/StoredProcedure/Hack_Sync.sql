CREATE OR ALTER PROCEDURE [DefiLlama].[Hack_Sync]
  @Hacks [DefiLlama].[HackRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[Hack] AS Tgt
  USING @Hacks AS Src ON Tgt.[Name] = Src.[Name] AND Tgt.[HackDate] = Src.[HackDate]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Classification] = Src.[Classification],
        [Technique] = Src.[Technique],
        [Amount] = Src.[Amount],
        [Source] = Src.[Source],
        [ParentProtocolId] = Src.[ParentProtocolId],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Name], [HackDate], [Classification], [Technique], [Amount], [Source], [ParentProtocolId], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Name], Src.[HackDate], Src.[Classification], Src.[Technique], Src.[Amount], Src.[Source], Src.[ParentProtocolId], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
