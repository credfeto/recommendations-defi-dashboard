CREATE OR ALTER PROCEDURE [dbo].[ContractSecurity_GetByChainAndAddress]
  @Chain NVARCHAR(100),
  @Address NVARCHAR(100)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Chain],
    [Address],
    [ParentAddress],
    [IsOpenSource],
    [IsHoneypot],
    [IsProxy],
    [BuyTax],
    [SellTax],
    [TransferTax],
    [CannotBuy],
    [HoneypotWithSameCreator],
    [TokenName],
    [TokenSymbol],
    [CheckedAt]
  FROM [dbo].[ContractSecurity]
  WHERE [Chain] = @Chain
    AND [Address] = @Address;
END;
