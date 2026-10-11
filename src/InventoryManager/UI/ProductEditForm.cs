using InventoryManager.Models;

namespace InventoryManager.UI;

/// <summary>상품 등록/수정 다이얼로그</summary>
public class ProductEditForm : Form
{
    private readonly TextBox _txtCode = new() { Width = 220, MaxLength = 20 };
    private readonly TextBox _txtName = new() { Width = 220, MaxLength = 100 };
    private readonly ComboBox _cboCategory = new() { Width = 220 };
    private readonly NumericUpDown _numPrice = new() { Width = 220, Maximum = 999_999_999, ThousandsSeparator = true, Increment = 1000 };
    private readonly NumericUpDown _numSafety = new() { Width = 220, Maximum = 1_000_000 };

    public Product Product { get; }

    public ProductEditForm(Product? product, IEnumerable<string> categories)
    {
        Product = product ?? new Product();
        Text = product == null ? "상품 등록" : "상품 수정";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        _cboCategory.Items.AddRange(categories.Cast<object>().ToArray());

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        void Row(string label, Control c)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 12, 3) });
            layout.Controls.Add(c);
        }
        Row("상품코드", _txtCode);
        Row("상품명", _txtName);
        Row("분류", _cboCategory);
        Row("단가(원)", _numPrice);
        Row("안전재고", _numSafety);

        var btnOk = new Button { Text = "저장", Width = 90 };
        var btnCancel = new Button { Text = "취소", Width = 90, DialogResult = DialogResult.Cancel };
        btnOk.Click += (_, _) => Save();
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.AddRange(new Control[] { btnCancel, btnOk });
        layout.Controls.Add(buttons);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        if (product != null)
        {
            _txtCode.Text = product.ProductCode;
            _txtName.Text = product.ProductName;
            _cboCategory.Text = product.Category;
            _numPrice.Value = product.UnitPrice;
            _numSafety.Value = product.SafetyStock;
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_txtCode.Text) || string.IsNullOrWhiteSpace(_txtName.Text)
            || string.IsNullOrWhiteSpace(_cboCategory.Text))
        {
            MessageBox.Show("상품코드, 상품명, 분류는 필수입니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Product.ProductCode = _txtCode.Text.Trim().ToUpperInvariant();
        Product.ProductName = _txtName.Text.Trim();
        Product.Category = _cboCategory.Text.Trim();
        Product.UnitPrice = _numPrice.Value;
        Product.SafetyStock = (int)_numSafety.Value;
        DialogResult = DialogResult.OK;
    }
}
