CREATE OR ALTER PROCEDURE [DefiLlama].[PoolRewardToken_Sync]
  @Rows [DefiLlama].[PoolRewardTokenRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[PoolRewardToken] AS Tgt
  USING @Rows AS Src ON Tgt.[PoolId] = Src.[PoolId] AND Tgt.[SortOrder] = Src.[SortOrder]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [TokenAddress] = Src.[TokenAddress],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([PoolId], [SortOrder], [TokenAddress], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[PoolId], Src.[SortOrder], Src.[TokenAddress], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
