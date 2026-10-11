using InventoryManager.Data;

namespace InventoryManager.UI;

/// <summary>대시보드 탭: 요약 카드 + 분류별 집계</summary>
public class DashboardPanel : UserControl
{
    private readonly StockRepository _stock = new();
    private readonly Label _lblProducts = CreateValueLabel();
    private readonly Label _lblStock = CreateValueLabel();
    private readonly Label _lblValue = CreateValueLabel();
    private readonly Label _lblLow = CreateValueLabel();
    private readonly DataGridView _grid = UiHelper.CreateGrid();

    public DashboardPanel()
    {
        Dock = DockStyle.Fill;

        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 110, ColumnCount = 4, Padding = new Padding(6) };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(CreateCard("등록 상품", _lblProducts, Color.FromArgb(52, 101, 164)));
        cards.Controls.Add(CreateCard("총 재고 수량", _lblStock, Color.FromArgb(46, 139, 87)));
        cards.Controls.Add(CreateCard("재고 자산(원)", _lblValue, Color.FromArgb(120, 81, 169)));
        cards.Controls.Add(CreateCard("안전재고 미달", _lblLow, Color.FromArgb(178, 34, 34)));

        var title = new Label
        {
            Text = "분류별 재고 현황", Dock = DockStyle.Top, Height = 30,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold), Padding = new Padding(8, 8, 0, 0),
        };

        Controls.Add(_grid);
        Controls.Add(title);
        Controls.Add(cards);
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var s = await _stock.GetDashboardAsync();
            _lblProducts.Text = s.ProductCount.ToString("N0");
            _lblStock.Text = s.TotalStock.ToString("N0");
            _lblValue.Text = s.TotalValue.ToString("N0");
            _lblLow.Text = s.LowStockCount.ToString("N0");

            _grid.DataSource = s.Categories;
            UiHelper.ConfigureColumns(_grid,
                ("Category", "분류", null),
                ("ProductCount", "상품 수", "N0"),
                ("TotalStock", "재고 수량", "N0"),
                ("TotalValue", "재고 자산(원)", "N0"));
        }
        catch (Exception ex) { UiHelper.ShowError(ex); }
    }

    private static Label CreateValueLabel() => new()
    {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.White, Text = "-",
    };

    private static Panel CreateCard(string caption, Label value, Color color)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = color, Margin = new Padding(6) };
        panel.Controls.Add(value);
        panel.Controls.Add(new Label
        {
            Text = caption, Dock = DockStyle.Top, Height = 26, ForeColor = Color.White,
            Padding = new Padding(8, 6, 0, 0),
        });
        return panel;
    }
}
