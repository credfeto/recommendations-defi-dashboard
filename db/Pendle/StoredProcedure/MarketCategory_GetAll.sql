CREATE OR ALTER PROCEDURE [Pendle].[MarketCategory_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Address],
    [ChainId],
    [CategoryId],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [Pendle].[MarketCategory]
  ORDER BY [Address], [ChainId], [CategoryId];
END;
