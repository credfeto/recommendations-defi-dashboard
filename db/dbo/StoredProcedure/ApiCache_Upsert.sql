CREATE OR ALTER PROCEDURE [dbo].[ApiCache_Upsert]
  @Key NVARCHAR(450),
  @Data NVARCHAR(MAX),
  @FetchedAt DATETIMEOFFSET
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [dbo].[ApiCache] AS Tgt
  USING (SELECT @Key AS [Key]) AS Src ON Tgt.[Key] = Src.[Key]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Data] = @Data,
        [FetchedAt] = @FetchedAt
  WHEN NOT MATCHED
    THEN
    INSERT ([Key], [Data], [FetchedAt])
    VALUES (@Key, @Data, @FetchedAt);
END;
