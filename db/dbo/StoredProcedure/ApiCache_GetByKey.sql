CREATE OR ALTER PROCEDURE [dbo].[ApiCache_GetByKey]
  @Key NVARCHAR(450)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Key],
    [Data],
    [FetchedAt]
  FROM [dbo].[ApiCache]
  WHERE [Key] = @Key;
END;
