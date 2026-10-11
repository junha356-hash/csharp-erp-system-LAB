-- =============================================
-- 재고관리 시스템 DB 스키마
-- =============================================
IF DB_ID(N'InventoryDB') IS NULL
    CREATE DATABASE InventoryDB;
GO

USE InventoryDB;
GO

-- 기존 객체 정리 (재실행 가능하도록)
IF OBJECT_ID(N'dbo.usp_StockInOut', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_StockInOut;
IF OBJECT_ID(N'dbo.usp_GetDashboard', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetDashboard;
IF OBJECT_ID(N'dbo.vw_LowStock', N'V') IS NOT NULL DROP VIEW dbo.vw_LowStock;
IF OBJECT_ID(N'dbo.StockHistory', N'U') IS NOT NULL DROP TABLE dbo.StockHistory;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
GO

-- 상품 마스터
CREATE TABLE dbo.Products (
    ProductId    INT IDENTITY(1,1) PRIMARY KEY,
    ProductCode  NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Products_Code UNIQUE,
    ProductName  NVARCHAR(100) NOT NULL,
    Category     NVARCHAR(50)  NOT NULL,
    UnitPrice    DECIMAL(12,0) NOT NULL CONSTRAINT CK_Products_Price CHECK (UnitPrice >= 0),
    Stock        INT           NOT NULL CONSTRAINT DF_Products_Stock DEFAULT (0)
                               CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
    SafetyStock  INT           NOT NULL CONSTRAINT DF_Products_Safety DEFAULT (0),
    CreatedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_Products_Created DEFAULT (SYSDATETIME()),
    UpdatedAt    DATETIME2(0)  NULL
);
GO

-- 입출고 이력
CREATE TABLE dbo.StockHistory (
    HistoryId    BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProductId    INT          NOT NULL CONSTRAINT FK_History_Product
                              REFERENCES dbo.Products(ProductId) ON DELETE CASCADE,
    IoType       CHAR(3)      NOT NULL CONSTRAINT CK_History_Type CHECK (IoType IN ('IN', 'OUT')),
    Quantity     INT          NOT NULL CONSTRAINT CK_History_Qty CHECK (Quantity > 0),
    StockAfter   INT          NOT NULL,
    Memo         NVARCHAR(200) NULL,
    CreatedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_History_Created DEFAULT (SYSDATETIME())
);
CREATE INDEX IX_History_Product_Date ON dbo.StockHistory(ProductId, CreatedAt DESC);
GO

-- 안전재고 미달 상품 뷰
CREATE VIEW dbo.vw_LowStock
AS
SELECT ProductId, ProductCode, ProductName, Category, Stock, SafetyStock,
       SafetyStock - Stock AS Shortage
FROM dbo.Products
WHERE Stock < SafetyStock;
GO

-- 입출고 처리 프로시저 (트랜잭션 + 행 잠금으로 동시성 제어)
CREATE PROCEDURE dbo.usp_StockInOut
    @ProductId INT,
    @IoType    CHAR(3),
    @Quantity  INT,
    @Memo      NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @IoType NOT IN ('IN', 'OUT') THROW 50001, N'입출고 구분이 올바르지 않습니다.', 1;
    IF @Quantity <= 0               THROW 50002, N'수량은 1 이상이어야 합니다.', 1;

    BEGIN TRAN;

    DECLARE @Stock INT;
    SELECT @Stock = Stock
    FROM dbo.Products WITH (UPDLOCK, ROWLOCK)
    WHERE ProductId = @ProductId;

    IF @Stock IS NULL
    BEGIN
        ROLLBACK;
        THROW 50003, N'존재하지 않는 상품입니다.', 1;
    END

    IF @IoType = 'OUT' AND @Stock < @Quantity
    BEGIN
        ROLLBACK;
        DECLARE @msg NVARCHAR(200) = CONCAT(N'재고가 부족합니다. (현재 재고: ', @Stock, N')');
        THROW 50004, @msg, 1;
    END

    DECLARE @NewStock INT = @Stock + CASE WHEN @IoType = 'IN' THEN @Quantity ELSE -@Quantity END;

    UPDATE dbo.Products
    SET Stock = @NewStock, UpdatedAt = SYSDATETIME()
    WHERE ProductId = @ProductId;

    INSERT INTO dbo.StockHistory (ProductId, IoType, Quantity, StockAfter, Memo)
    VALUES (@ProductId, @IoType, @Quantity, @NewStock, @Memo);

    COMMIT;

    SELECT @NewStock AS NewStock;
END
GO

-- 대시보드 요약 프로시저 (다중 결과셋)
CREATE PROCEDURE dbo.usp_GetDashboard
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)                         AS ProductCount,
           ISNULL(SUM(Stock), 0)            AS TotalStock,
           ISNULL(SUM(Stock * UnitPrice), 0) AS TotalValue,
           (SELECT COUNT(*) FROM dbo.vw_LowStock) AS LowStockCount
    FROM dbo.Products;

    SELECT Category,
           COUNT(*)               AS ProductCount,
           SUM(Stock)             AS TotalStock,
           SUM(Stock * UnitPrice) AS TotalValue
    FROM dbo.Products
    GROUP BY Category
    ORDER BY TotalValue DESC;
END
GO
