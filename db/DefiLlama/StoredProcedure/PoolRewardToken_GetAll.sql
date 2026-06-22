CREATE OR ALTER PROCEDURE [DefiLlama].[PoolRewardToken_GetAll]
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
  FROM [DefiLlama].[PoolRewardToken]
  ORDER BY [PoolId], [SortOrder];
END;
