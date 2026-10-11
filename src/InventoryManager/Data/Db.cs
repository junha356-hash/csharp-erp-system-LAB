using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace InventoryManager.Data;

/// <summary>
/// appsettings.json 의 연결 문자열로 SqlConnection 을 생성한다.
/// </summary>
public static class Db
{
    private static readonly Lazy<string> _connectionString = new(LoadConnectionString);

    public static async Task<SqlConnection> OpenAsync()
    {
        var conn = new SqlConnection(_connectionString.Value);
        await conn.OpenAsync();
        return conn;
    }

    private static string LoadConnectionString()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("ConnectionStrings").GetProperty("InventoryDB").GetString()
               ?? throw new InvalidOperationException("연결 문자열이 없습니다.");
    }
}
