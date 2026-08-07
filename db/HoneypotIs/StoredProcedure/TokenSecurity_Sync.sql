CREATE OR ALTER PROCEDURE [HoneypotIs].[TokenSecurity_Sync]
  @Rows [HoneypotIs].[TokenSecurityRow] READONLY
AS
BEGIN
  SET NOCOUNT ON;

  -- No WHEN NOT MATCHED BY SOURCE / DELETE branch: this is an on-demand per-key cache
  -- (SetHoneypotIsAsync writes one contract at a time), not a bulk snapshot sync - see
  -- the TVP Sync Pattern exception in ai/local/database.instructions.md.
  MERGE [HoneypotIs].[TokenSecurity] AS Tgt
  USING @Rows AS Src ON Tgt.[Chain] = Src.[Chain] AND Tgt.[Address] = Src.[Address]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [IsHoneypot] = Src.[IsHoneypot],
        [BuyTax] = Src.[BuyTax],
        [SellTax] = Src.[SellTax],
        [SimulationSuccess] = Src.[SimulationSuccess],
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [Chain],
      [Address],
      [IsHoneypot],
      [BuyTax],
      [SellTax],
      [SimulationSuccess],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Chain],
      Src.[Address],
      Src.[IsHoneypot],
      Src.[BuyTax],
      Src.[SellTax],
      Src.[SimulationSuccess],
      NULL,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    );
END;
