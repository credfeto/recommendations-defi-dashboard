CREATE OR ALTER PROCEDURE [GoPlus].[TokenSecurity_GetChildrenByParentAddress]
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
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [GoPlus].[TokenSecurity]
  WHERE [Chain] = @Chain
    AND [ParentAddress] = @ParentAddress;
END;
