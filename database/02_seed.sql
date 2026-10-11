-- =============================================
-- 샘플 데이터
-- =============================================
USE InventoryDB;
GO

INSERT INTO dbo.Products (ProductCode, ProductName, Category, UnitPrice, SafetyStock) VALUES
(N'P-001', N'마우스',  N'주변기기', 20000, 10),
(N'P-002', N'키보드',  N'주변기기', 50000, 10),
(N'P-003', N'웹캠',    N'주변기기', 30000, 5),
(N'P-004', N'모니터',  N'디스플레이', 200000, 5),
(N'P-005', N'헤드셋',  N'음향기기', 80000, 5),
(N'P-006', N'스피커',  N'음향기기', 40000, 5);
GO

-- 입출고는 프로시저로 처리 (재고 갱신 + 이력 기록)
EXEC dbo.usp_StockInOut 1, 'IN', 50, N'입고';
EXEC dbo.usp_StockInOut 2, 'IN', 30, N'입고';
EXEC dbo.usp_StockInOut 3, 'IN', 10, N'입고';
EXEC dbo.usp_StockInOut 4, 'IN', 10, N'입고';
EXEC dbo.usp_StockInOut 5, 'IN', 10, N'입고';
EXEC dbo.usp_StockInOut 6, 'IN', 20, N'입고';
EXEC dbo.usp_StockInOut 1, 'OUT', 42, N'납품';
EXEC dbo.usp_StockInOut 2, 'OUT', 10, N'납품';
EXEC dbo.usp_StockInOut 4, 'OUT', 7,  N'납품';
EXEC dbo.usp_StockInOut 3, 'OUT', 7,  N'납품';
EXEC dbo.usp_StockInOut 6, 'OUT', 8,  N'납품';
EXEC dbo.usp_StockInOut 2, 'IN', 5,   N'입고';
GO

-- 데모용: 이력 일시를 최근 며칠에 나눠서 표시
UPDATE dbo.StockHistory
SET CreatedAt = DATEADD(HOUR, 9 + HistoryId % 8, CAST(CAST(DATEADD(DAY, (HistoryId - 13) / 2, SYSDATETIME()) AS DATE) AS DATETIME2(0)));
UPDATE dbo.Products SET CreatedAt = DATEADD(DAY, -7, CreatedAt);
GO
