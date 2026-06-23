CREATE OR ALTER PROCEDURE [dbo].[ContractSecurity_GetChildrenByParentAddress]
  @Chain NVARCHAR(100),
  @ParentAddress NVARCHAR(100)
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
    AND [ParentAddress] = @ParentAddress;
END;
