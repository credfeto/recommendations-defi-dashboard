IF OBJECT_ID(N'[dbo].[ApiCache]', N'U') IS NULL
  BEGIN
    CREATE TABLE [dbo].[ApiCache]
    (
      [Key] NVARCHAR(450) NOT NULL,
      [Data] NVARCHAR(MAX) NOT NULL,
      [FetchedAt] DATETIMEOFFSET NOT NULL,
      CONSTRAINT [PK_ApiCache] PRIMARY KEY ([Key])
    );
  END;
GO

IF OBJECT_ID(N'[dbo].[ContractSecurity]', N'U') IS NULL
  BEGIN
    CREATE TABLE [dbo].[ContractSecurity]
    (
      [Chain] NVARCHAR(100) NOT NULL,
      [Address] NVARCHAR(100) NOT NULL,
      [ParentAddress] NVARCHAR(100) NULL,
      [IsOpenSource] BIT NULL,
      [IsHoneypot] BIT NULL,
      [IsProxy] BIT NULL,
      [BuyTax] FLOAT(53) NULL,
      [SellTax] FLOAT(53) NULL,
      [TransferTax] FLOAT(53) NULL,
      [CannotBuy] BIT NULL,
      [HoneypotWithSameCreator] BIT NULL,
      [TokenName] NVARCHAR(MAX) NULL,
      [TokenSymbol] NVARCHAR(MAX) NULL,
      [CheckedAt] DATETIMEOFFSET NOT NULL,
      CONSTRAINT [PK_ContractSecurity] PRIMARY KEY ([Chain], [Address])
    );
  END;
GO

CREATE OR ALTER PROCEDURE [dbo].[ApiCache_GetByKey]
  @Key NVARCHAR(450)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Key],
    [Data],
    [FetchedAt]
  FROM [dbo].[ApiCache]
  WHERE [Key] = @Key;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[ApiCache_Upsert]
  @Key NVARCHAR(450),
  @Data NVARCHAR(MAX),
  @FetchedAt DATETIMEOFFSET
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [dbo].[ApiCache] AS Tgt
  USING (SELECT @Key AS [Key]) AS Src ON Tgt.[Key] = Src.[Key]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Data] = @Data,
        [FetchedAt] = @FetchedAt
  WHEN NOT MATCHED
    THEN
    INSERT ([Key], [Data], [FetchedAt])
    VALUES (@Key, @Data, @FetchedAt);
END;
GO

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
GO

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
GO

CREATE OR ALTER PROCEDURE [dbo].[ContractSecurity_Upsert]
  @Chain NVARCHAR(100),
  @Address NVARCHAR(100),
  @ParentAddress NVARCHAR(100) = NULL,
  @IsOpenSource BIT = NULL,
  @IsHoneypot BIT = NULL,
  @IsProxy BIT = NULL,
  @BuyTax FLOAT(53) = NULL,
  @SellTax FLOAT(53) = NULL,
  @TransferTax FLOAT(53) = NULL,
  @CannotBuy BIT = NULL,
  @HoneypotWithSameCreator BIT = NULL,
  @TokenName NVARCHAR(MAX) = NULL,
  @TokenSymbol NVARCHAR(MAX) = NULL,
  @CheckedAt DATETIMEOFFSET
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [dbo].[ContractSecurity] AS Tgt
  USING (
    SELECT
      @Chain   AS [Chain],
      @Address AS [Address]
  ) AS Src
  ON Tgt.[Chain] = Src.[Chain] AND Tgt.[Address] = Src.[Address]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [ParentAddress] = @ParentAddress,
        [IsOpenSource] = @IsOpenSource,
        [IsHoneypot] = @IsHoneypot,
        [IsProxy] = @IsProxy,
        [BuyTax] = @BuyTax,
        [SellTax] = @SellTax,
        [TransferTax] = @TransferTax,
        [CannotBuy] = @CannotBuy,
        [HoneypotWithSameCreator] = @HoneypotWithSameCreator,
        [TokenName] = @TokenName,
        [TokenSymbol] = @TokenSymbol,
        [CheckedAt] = @CheckedAt
  WHEN NOT MATCHED
    THEN
    INSERT (
      [Chain], [Address], [ParentAddress], [IsOpenSource], [IsHoneypot], [IsProxy],
      [BuyTax], [SellTax], [TransferTax], [CannotBuy], [HoneypotWithSameCreator],
      [TokenName], [TokenSymbol], [CheckedAt]
    )
    VALUES (
      @Chain, @Address, @ParentAddress, @IsOpenSource, @IsHoneypot, @IsProxy,
      @BuyTax, @SellTax, @TransferTax, @CannotBuy, @HoneypotWithSameCreator,
      @TokenName, @TokenSymbol, @CheckedAt
    );
END;
GO
