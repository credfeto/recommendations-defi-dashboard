CREATE OR ALTER PROCEDURE [DefiLlama].[Pool_Sync]
  @Rows [DefiLlama].[PoolRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[Pool] AS Tgt
  USING @Rows AS Src ON Tgt.[PoolId] = Src.[PoolId]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Chain] = Src.[Chain],
        [Project] = Src.[Project],
        [Symbol] = Src.[Symbol],
        [TvlUsd] = Src.[TvlUsd],
        [ApyBase] = Src.[ApyBase],
        [ApyReward] = Src.[ApyReward],
        [Apy] = Src.[Apy],
        [ApyPct1D] = Src.[ApyPct1D],
        [ApyPct7D] = Src.[ApyPct7D],
        [ApyPct30D] = Src.[ApyPct30D],
        [Stablecoin] = Src.[Stablecoin],
        [IlRisk] = Src.[IlRisk],
        [Exposure] = Src.[Exposure],
        [PredictedClass] = Src.[PredictedClass],
        [PredictedProbability] = Src.[PredictedProbability],
        [BinnedConfidence] = Src.[BinnedConfidence],
        [PoolMeta] = Src.[PoolMeta],
        [Mu] = Src.[Mu],
        [Sigma] = Src.[Sigma],
        [Count] = Src.[Count],
        [Outlier] = Src.[Outlier],
        [Il7d] = Src.[Il7d],
        [ApyBase7d] = Src.[ApyBase7d],
        [ApyMean30d] = Src.[ApyMean30d],
        [VolumeUsd1d] = Src.[VolumeUsd1d],
        [VolumeUsd7d] = Src.[VolumeUsd7d],
        [ApyBaseInception] = Src.[ApyBaseInception],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [PoolId],
      [Chain],
      [Project],
      [Symbol],
      [TvlUsd],
      [ApyBase],
      [ApyReward],
      [Apy],
      [ApyPct1D],
      [ApyPct7D],
      [ApyPct30D],
      [Stablecoin],
      [IlRisk],
      [Exposure],
      [PredictedClass],
      [PredictedProbability],
      [BinnedConfidence],
      [PoolMeta],
      [Mu],
      [Sigma],
      [Count],
      [Outlier],
      [Il7d],
      [ApyBase7d],
      [ApyMean30d],
      [VolumeUsd1d],
      [VolumeUsd7d],
      [ApyBaseInception],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[PoolId],
      Src.[Chain],
      Src.[Project],
      Src.[Symbol],
      Src.[TvlUsd],
      Src.[ApyBase],
      Src.[ApyReward],
      Src.[Apy],
      Src.[ApyPct1D],
      Src.[ApyPct7D],
      Src.[ApyPct30D],
      Src.[Stablecoin],
      Src.[IlRisk],
      Src.[Exposure],
      Src.[PredictedClass],
      Src.[PredictedProbability],
      Src.[BinnedConfidence],
      Src.[PoolMeta],
      Src.[Mu],
      Src.[Sigma],
      Src.[Count],
      Src.[Outlier],
      Src.[Il7d],
      Src.[ApyBase7d],
      Src.[ApyMean30d],
      Src.[VolumeUsd1d],
      Src.[VolumeUsd7d],
      Src.[ApyBaseInception],
      @DataDate,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    )
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
