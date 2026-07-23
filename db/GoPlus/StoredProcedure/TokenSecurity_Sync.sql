CREATE OR ALTER PROCEDURE [GoPlus].[TokenSecurity_Sync]
  @Rows [GoPlus].[TokenSecurityRow] READONLY
AS
BEGIN
  SET NOCOUNT ON;

  -- No WHEN NOT MATCHED BY SOURCE / DELETE branch: this is an on-demand per-key cache
  -- (SetAsync writes one contract at a time), not a bulk snapshot sync - see the TVP
  -- Sync Pattern exception in ai/local/database.instructions.md.
  MERGE [GoPlus].[TokenSecurity] AS Tgt
  USING @Rows AS Src ON Tgt.[Chain] = Src.[Chain] AND Tgt.[Address] = Src.[Address]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [ParentAddress] = Src.[ParentAddress],
        [IsOpenSource] = Src.[IsOpenSource],
        [IsHoneypot] = Src.[IsHoneypot],
        [IsProxy] = Src.[IsProxy],
        [BuyTax] = Src.[BuyTax],
        [SellTax] = Src.[SellTax],
        [TransferTax] = Src.[TransferTax],
        [CannotBuy] = Src.[CannotBuy],
        [HoneypotWithSameCreator] = Src.[HoneypotWithSameCreator],
        [TokenName] = Src.[TokenName],
        [TokenSymbol] = Src.[TokenSymbol],
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
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
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Chain],
      Src.[Address],
      Src.[ParentAddress],
      Src.[IsOpenSource],
      Src.[IsHoneypot],
      Src.[IsProxy],
      Src.[BuyTax],
      Src.[SellTax],
      Src.[TransferTax],
      Src.[CannotBuy],
      Src.[HoneypotWithSameCreator],
      Src.[TokenName],
      Src.[TokenSymbol],
      NULL,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    );
END;
