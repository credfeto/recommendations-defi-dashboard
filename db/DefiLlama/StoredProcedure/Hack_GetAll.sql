CREATE OR ALTER PROCEDURE [DefiLlama].[Hack_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Name],
    [HackDate],
    [Classification],
    [Technique],
    [Amount],
    [Source],
    [ParentProtocolId],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[Hack];
END;
