CREATE OR ALTER PROCEDURE [DefiLlama].[Pool_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
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
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[Pool];
END;
