CREATE OR ALTER PROCEDURE [HoneypotIs].[TokenSecurity_GetByChainAndAddress]
  @Chain NVARCHAR(100),
  @Address NVARCHAR(100)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Chain],
    [Address],
    [IsHoneypot],
    [BuyTax],
    [SellTax],
    [SimulationSuccess],
    [DateCreated],
    [DateUpdated]
  FROM [HoneypotIs].[TokenSecurity]
  WHERE [Chain] = @Chain
    AND [Address] = @Address;
END;
