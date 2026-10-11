using System.Data;
using InventoryManager.Models;
using Microsoft.Data.SqlClient;

namespace InventoryManager.Data;

/// <summary>
/// 입출고 처리 및 이력/대시보드 조회. 재고 변경은 저장 프로시저(usp_StockInOut)에서 트랜잭션으로 처리한다.
/// </summary>
public class StockRepository
{
    public async Task<int> StockInOutAsync(int productId, string ioType, int quantity, string? memo)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = new SqlCommand("dbo.usp_StockInOut", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
        cmd.Parameters.Add("@IoType", SqlDbType.Char, 3).Value = ioType;
        cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;
        cmd.Parameters.Add("@Memo", SqlDbType.NVarChar, 200).Value =
            string.IsNullOrWhiteSpace(memo) ? DBNull.Value : memo.Trim();

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<List<StockHistoryItem>> GetHistoryAsync(DateTime from, DateTime to, string? ioType)
    {
        const string sql = """
            SELECT h.HistoryId, p.ProductCode, p.ProductName, h.IoType, h.Quantity, h.StockAfter, h.Memo, h.CreatedAt
            FROM dbo.StockHistory h
            JOIN dbo.Products p ON p.ProductId = h.ProductId
            WHERE h.CreatedAt >= @From AND h.CreatedAt < @To
              AND (@IoType IS NULL OR h.IoType = @IoType)
            ORDER BY h.CreatedAt DESC, h.HistoryId DESC;
            """;

        await using var conn = await Db.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@From", SqlDbType.DateTime2).Value = from.Date;
        cmd.Parameters.Add("@To", SqlDbType.DateTime2).Value = to.Date.AddDays(1);
        cmd.Parameters.Add("@IoType", SqlDbType.Char, 3).Value = (object?)ioType ?? DBNull.Value;

        var list = new List<StockHistoryItem>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new StockHistoryItem
            {
                HistoryId = reader.GetInt64(0),
                ProductCode = reader.GetString(1),
                ProductName = reader.GetString(2),
                IoType = reader.GetString(3).Trim() == "IN" ? "입고" : "출고",   // CHAR(3) 이므로 공백 패딩 제거
                Quantity = reader.GetInt32(4),
                StockAfter = reader.GetInt32(5),
                Memo = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
            });
        }
        return list;
    }

    public async Task<DashboardSummary> GetDashboardAsync()
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = new SqlCommand("dbo.usp_GetDashboard", conn) { CommandType = CommandType.StoredProcedure };
        await using var reader = await cmd.ExecuteReaderAsync();

        var summary = new DashboardSummary();
        if (await reader.ReadAsync())
        {
            summary.ProductCount = reader.GetInt32(0);
            summary.TotalStock = reader.GetInt32(1);
            summary.TotalValue = reader.GetDecimal(2);
            summary.LowStockCount = reader.GetInt32(3);
        }

        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.Categories.Add(new CategorySummary
            {
                Category = reader.GetString(0),
                ProductCount = reader.GetInt32(1),
                TotalStock = reader.GetInt32(2),
                TotalValue = reader.GetDecimal(3),
            });
        }
        return summary;
    }
}
