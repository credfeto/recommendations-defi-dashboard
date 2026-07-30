CREATE OR ALTER PROCEDURE [DefiLlama].[Protocol_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Slug],
    [Audits],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[Protocol];
END;
