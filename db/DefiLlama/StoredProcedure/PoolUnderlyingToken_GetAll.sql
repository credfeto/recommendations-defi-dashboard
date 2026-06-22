CREATE OR ALTER PROCEDURE [DefiLlama].[PoolUnderlyingToken_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [PoolId],
    [SortOrder],
    [TokenAddress],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[PoolUnderlyingToken]
  ORDER BY [PoolId], [SortOrder];
END;
