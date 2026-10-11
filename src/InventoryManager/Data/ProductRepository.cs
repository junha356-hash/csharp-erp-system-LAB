using System.Data;
using InventoryManager.Models;
using Microsoft.Data.SqlClient;

namespace InventoryManager.Data;

/// <summary>
/// 상품 CRUD. 모든 쿼리는 파라미터 바인딩으로 SQL Injection 을 방지한다.
/// </summary>
public class ProductRepository
{
    public async Task<List<Product>> SearchAsync(string? keyword, string? category, bool lowStockOnly)
    {
        const string sql = """
            SELECT ProductId, ProductCode, ProductName, Category, UnitPrice, Stock, SafetyStock, CreatedAt
            FROM dbo.Products
            WHERE (@Keyword IS NULL OR ProductCode LIKE '%' + @Keyword + '%' OR ProductName LIKE '%' + @Keyword + '%')
              AND (@Category IS NULL OR Category = @Category)
              AND (@LowOnly = 0 OR Stock < SafetyStock)
            ORDER BY ProductCode;
            """;

        await using var conn = await Db.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(keyword) ? DBNull.Value : keyword.Trim();
        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value =
            string.IsNullOrWhiteSpace(category) ? DBNull.Value : category;
        cmd.Parameters.Add("@LowOnly", SqlDbType.Bit).Value = lowStockOnly;

        var list = new List<Product>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Product
            {
                ProductId = reader.GetInt32(0),
                ProductCode = reader.GetString(1),
                ProductName = reader.GetString(2),
                Category = reader.GetString(3),
                UnitPrice = reader.GetDecimal(4),
                Stock = reader.GetInt32(5),
                SafetyStock = reader.GetInt32(6),
                CreatedAt = reader.GetDateTime(7),
            });
        }
        return list;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = new SqlCommand("SELECT DISTINCT Category FROM dbo.Products ORDER BY Category", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await reader.ReadAsync()) list.Add(reader.GetString(0));
        return list;
    }

    public async Task InsertAsync(Product p)
    {
        const string sql = """
            INSERT INTO dbo.Products (ProductCode, ProductName, Category, UnitPrice, SafetyStock)
            VALUES (@Code, @Name, @Category, @Price, @Safety);
            """;
        await ExecuteAsync(sql, p);
    }

    public async Task UpdateAsync(Product p)
    {
        const string sql = """
            UPDATE dbo.Products
            SET ProductCode = @Code, ProductName = @Name, Category = @Category,
                UnitPrice = @Price, SafetyStock = @Safety, UpdatedAt = SYSDATETIME()
            WHERE ProductId = @Id;
            """;
        await ExecuteAsync(sql, p);
    }

    public async Task DeleteAsync(int productId)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = new SqlCommand("DELETE FROM dbo.Products WHERE ProductId = @Id", conn);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = productId;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(string sql, Product p)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = p.ProductId;
        cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 20).Value = p.ProductCode;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = p.ProductName;
        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = p.Category;
        cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = p.UnitPrice;
        cmd.Parameters.Add("@Safety", SqlDbType.Int).Value = p.SafetyStock;
        await cmd.ExecuteNonQueryAsync();
    }
}
