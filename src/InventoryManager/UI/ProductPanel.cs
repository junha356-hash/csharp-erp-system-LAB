using InventoryManager.Data;
using InventoryManager.Models;

namespace InventoryManager.UI;

/// <summary>상품 관리 탭: 검색 / 등록 / 수정 / 삭제 / 입출고</summary>
public class ProductPanel : UserControl
{
    private readonly ProductRepository _products = new();
    private readonly StockRepository _stock = new();

    private readonly TextBox _txtKeyword = new() { Width = 180, PlaceholderText = "코드 또는 상품명" };
    private readonly ComboBox _cboCategory = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _chkLow = new() { Text = "재고부족만", AutoSize = true, Margin = new Padding(8, 6, 3, 3) };
    private readonly DataGridView _grid = UiHelper.CreateGrid();
    private readonly Label _lblCount = new() { AutoSize = true, Margin = new Padding(12, 8, 3, 3) };

    public event EventHandler? DataChanged;

    public ProductPanel()
    {
        Dock = DockStyle.Fill;

        var btnSearch = new Button { Text = "조회", Width = 70 };
        var btnAdd = new Button { Text = "등록", Width = 70 };
        var btnEdit = new Button { Text = "수정", Width = 70 };
        var btnDelete = new Button { Text = "삭제", Width = 70 };
        var btnIn = new Button { Text = "입고", Width = 70, BackColor = Color.FromArgb(220, 237, 255) };
        var btnOut = new Button { Text = "출고", Width = 70, BackColor = Color.FromArgb(255, 228, 225) };

        btnSearch.Click += async (_, _) => await LoadDataAsync();
        _txtKeyword.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await LoadDataAsync(); };
        btnAdd.Click += async (_, _) => await AddAsync();
        btnEdit.Click += async (_, _) => await EditAsync();
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await EditAsync(); };
        btnDelete.Click += async (_, _) => await DeleteAsync();
        btnIn.Click += async (_, _) => await StockAsync("IN");
        btnOut.Click += async (_, _) => await StockAsync("OUT");
        _grid.CellFormatting += Grid_CellFormatting;

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(4, 6, 4, 0) };
        toolbar.Controls.AddRange(new Control[]
        {
            _txtKeyword, _cboCategory, _chkLow, btnSearch,
            new Label { Width = 20 }, btnAdd, btnEdit, btnDelete,
            new Label { Width = 20 }, btnIn, btnOut, _lblCount,
        });

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    public async Task LoadDataAsync()
    {
        try
        {
            await RefreshCategoriesAsync();
            var category = _cboCategory.SelectedIndex > 0 ? _cboCategory.SelectedItem as string : null;
            var list = await _products.SearchAsync(_txtKeyword.Text, category, _chkLow.Checked);

            _grid.DataSource = list;
            UiHelper.ConfigureColumns(_grid,
                ("ProductCode", "상품코드", null),
                ("ProductName", "상품명", null),
                ("Category", "분류", null),
                ("UnitPrice", "단가", "N0"),
                ("Stock", "현재고", "N0"),
                ("SafetyStock", "안전재고", "N0"),
                ("CreatedAt", "등록일", "yyyy-MM-dd"));
            _lblCount.Text = $"총 {list.Count}건";
        }
        catch (Exception ex) { UiHelper.ShowError(ex); }
    }

    private async Task RefreshCategoriesAsync()
    {
        var selected = _cboCategory.SelectedItem as string;
        var categories = await _products.GetCategoriesAsync();
        _cboCategory.Items.Clear();
        _cboCategory.Items.Add("(전체 분류)");
        _cboCategory.Items.AddRange(categories.Cast<object>().ToArray());
        var idx = selected == null ? 0 : _cboCategory.Items.IndexOf(selected);
        _cboCategory.SelectedIndex = Math.Max(idx, 0);
    }

    // 안전재고 미달 행은 빨간색으로 강조
    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Rows[e.RowIndex].DataBoundItem is Product { IsLowStock: true } && e.CellStyle != null)
        {
            e.CellStyle.ForeColor = Color.Firebrick;
            if (_grid.Columns[e.ColumnIndex].Name == "Stock")
                e.CellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
        }
    }

    private Product? Selected
    {
        get
        {
            if (_grid.SelectedRows.Count > 0) return _grid.SelectedRows[0].DataBoundItem as Product;
            MessageBox.Show("상품을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
    }

    private async Task AddAsync()
    {
        using var dlg = new ProductEditForm(null, await _products.GetCategoriesAsync());
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _products.InsertAsync(dlg.Product));
    }

    private async Task EditAsync()
    {
        if (Selected is not { } p) return;
        using var dlg = new ProductEditForm(p, await _products.GetCategoriesAsync());
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _products.UpdateAsync(dlg.Product));
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } p) return;
        var answer = MessageBox.Show($"[{p.ProductCode}] {p.ProductName} 상품을 삭제할까요?\n입출고 이력도 함께 삭제됩니다.",
            "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        await RunAsync(() => _products.DeleteAsync(p.ProductId));
    }

    private async Task StockAsync(string ioType)
    {
        if (Selected is not { } p) return;
        using var dlg = new StockDialog(p, ioType);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _stock.StockInOutAsync(p.ProductId, ioType, dlg.Quantity, dlg.Memo));
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            await LoadDataAsync();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { UiHelper.ShowError(ex); }
    }
}

/// <summary>입고/출고 수량 입력 다이얼로그</summary>
public class StockDialog : Form
{
    private readonly NumericUpDown _numQty = new() { Width = 200, Minimum = 1, Maximum = 100_000, Value = 1 };
    private readonly TextBox _txtMemo = new() { Width = 200, MaxLength = 200 };

    public int Quantity => (int)_numQty.Value;
    public string Memo => _txtMemo.Text;

    public StockDialog(Product p, string ioType)
    {
        Text = ioType == "IN" ? "입고 처리" : "출고 처리";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
        void Row(string label, Control c)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 8, 12, 3) });
            layout.Controls.Add(c);
        }
        Row("상품", new Label { Text = $"[{p.ProductCode}] {p.ProductName}", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        Row("현재고", new Label { Text = p.Stock.ToString("N0"), AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        Row("수량", _numQty);
        Row("비고", _txtMemo);

        var btnOk = new Button { Text = "확인", Width = 90, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "취소", Width = 90, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.AddRange(new Control[] { btnCancel, btnOk });
        layout.Controls.Add(buttons);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
