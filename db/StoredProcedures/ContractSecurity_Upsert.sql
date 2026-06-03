CREATE OR ALTER PROCEDURE [dbo].[ContractSecurity_Upsert]
    @Chain                   NVARCHAR(100),
    @Address                 NVARCHAR(100),
    @ParentAddress           NVARCHAR(100)  = NULL,
    @IsOpenSource            BIT            = NULL,
    @IsHoneypot              BIT            = NULL,
    @IsProxy                 BIT            = NULL,
    @BuyTax                  FLOAT(53)      = NULL,
    @SellTax                 FLOAT(53)      = NULL,
    @TransferTax             FLOAT(53)      = NULL,
    @CannotBuy               BIT            = NULL,
    @HoneypotWithSameCreator BIT            = NULL,
    @TokenName               NVARCHAR(MAX)  = NULL,
    @TokenSymbol             NVARCHAR(MAX)  = NULL,
    @CheckedAt               DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    MERGE [dbo].[ContractSecurity] AS target
    USING (SELECT @Chain AS [Chain], @Address AS [Address]) AS source
        ON target.[Chain] = source.[Chain] AND target.[Address] = source.[Address]
    WHEN MATCHED THEN
        UPDATE SET [ParentAddress]           = @ParentAddress,
                   [IsOpenSource]            = @IsOpenSource,
                   [IsHoneypot]              = @IsHoneypot,
                   [IsProxy]                 = @IsProxy,
                   [BuyTax]                  = @BuyTax,
                   [SellTax]                 = @SellTax,
                   [TransferTax]             = @TransferTax,
                   [CannotBuy]               = @CannotBuy,
                   [HoneypotWithSameCreator] = @HoneypotWithSameCreator,
                   [TokenName]               = @TokenName,
                   [TokenSymbol]             = @TokenSymbol,
                   [CheckedAt]               = @CheckedAt
    WHEN NOT MATCHED THEN
        INSERT ([Chain], [Address], [ParentAddress], [IsOpenSource], [IsHoneypot], [IsProxy],
                [BuyTax], [SellTax], [TransferTax], [CannotBuy], [HoneypotWithSameCreator],
                [TokenName], [TokenSymbol], [CheckedAt])
        VALUES (@Chain, @Address, @ParentAddress, @IsOpenSource, @IsHoneypot, @IsProxy,
                @BuyTax, @SellTax, @TransferTax, @CannotBuy, @HoneypotWithSameCreator,
                @TokenName, @TokenSymbol, @CheckedAt);
END;
