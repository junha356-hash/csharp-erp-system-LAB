using InventoryManager.Data;
using InventoryManager.Models;

namespace InventoryManager.UI;

/// <summary>입출고 이력 탭: 기간 / 구분 조회</summary>
public class HistoryPanel : UserControl
{
    private readonly StockRepository _stock = new();
    private readonly DateTimePicker _dtFrom = new() { Width = 120, Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _dtTo = new() { Width = 120, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _cboType = new() { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid = UiHelper.CreateGrid();
    private readonly Label _lblSummary = new() { AutoSize = true, Margin = new Padding(12, 8, 3, 3) };

    public HistoryPanel()
    {
        Dock = DockStyle.Fill;
        _dtFrom.Value = DateTime.Today.AddDays(-30);
        _cboType.Items.AddRange(new object[] { "전체", "입고", "출고" });
        _cboType.SelectedIndex = 0;

        var btnSearch = new Button { Text = "조회", Width = 70 };
        btnSearch.Click += async (_, _) => await LoadDataAsync();
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Rows[e.RowIndex].DataBoundItem is StockHistoryItem item && e.CellStyle != null
                && _grid.Columns[e.ColumnIndex].Name == "IoType")
                e.CellStyle.ForeColor = item.IoType == "입고" ? Color.RoyalBlue : Color.Firebrick;
        };

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(4, 6, 4, 0) };
        toolbar.Controls.AddRange(new Control[]
        {
            new Label { Text = "기간", AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, _dtFrom,
            new Label { Text = "~", AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, _dtTo,
            _cboType, btnSearch, _lblSummary,
        });

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    public async Task LoadDataAsync()
    {
        try
        {
            string? type = _cboType.SelectedIndex switch { 1 => "IN", 2 => "OUT", _ => null };
            var list = await _stock.GetHistoryAsync(_dtFrom.Value, _dtTo.Value, type);

            _grid.DataSource = list;
            UiHelper.ConfigureColumns(_grid,
                ("CreatedAt", "일시", "yyyy-MM-dd HH:mm"),
                ("IoType", "구분", null),
                ("ProductCode", "상품코드", null),
                ("ProductName", "상품명", null),
                ("Quantity", "수량", "N0"),
                ("StockAfter", "처리 후 재고", "N0"),
                ("Memo", "비고", null));
            _grid.Columns["CreatedAt"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            var inQty = list.Where(x => x.IoType == "입고").Sum(x => x.Quantity);
            var outQty = list.Where(x => x.IoType == "출고").Sum(x => x.Quantity);
            _lblSummary.Text = $"{list.Count}건  |  입고 {inQty:N0}  /  출고 {outQty:N0}";
        }
        catch (Exception ex) { UiHelper.ShowError(ex); }
    }
}
