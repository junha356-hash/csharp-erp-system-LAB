namespace InventoryManager.Models;

public class Product
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int SafetyStock { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsLowStock => Stock < SafetyStock;
}

public class StockHistoryItem
{
    public long HistoryId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string IoType { get; set; } = "";
    public int Quantity { get; set; }
    public int StockAfter { get; set; }
    public string? Memo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardSummary
{
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
    public List<CategorySummary> Categories { get; } = new();
}

public class CategorySummary
{
    public string Category { get; set; } = "";
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalValue { get; set; }
}
