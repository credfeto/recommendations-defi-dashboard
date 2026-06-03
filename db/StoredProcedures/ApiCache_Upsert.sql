CREATE OR ALTER PROCEDURE [dbo].[ApiCache_Upsert]
    @Key       NVARCHAR(450),
    @Data      NVARCHAR(MAX),
    @FetchedAt DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    MERGE [dbo].[ApiCache] AS target
    USING (SELECT @Key AS [Key]) AS source ON target.[Key] = source.[Key]
    WHEN MATCHED THEN
        UPDATE SET [Data]      = @Data,
                   [FetchedAt] = @FetchedAt
    WHEN NOT MATCHED THEN
        INSERT ([Key], [Data], [FetchedAt])
        VALUES (@Key, @Data, @FetchedAt);
END;
