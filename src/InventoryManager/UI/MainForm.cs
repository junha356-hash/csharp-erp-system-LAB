namespace InventoryManager.UI;

public class MainForm : Form
{
    private readonly DashboardPanel _dashboard = new();
    private readonly ProductPanel _products = new();
    private readonly HistoryPanel _history = new();

    public MainForm()
    {
        Text = "재고관리 시스템";
        Size = new Size(1100, 700);
        MinimumSize = new Size(900, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("맑은 고딕", 9F);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };
        tabs.TabPages.Add(CreateTab("대시보드", _dashboard));
        tabs.TabPages.Add(CreateTab("상품 / 재고 관리", _products));
        tabs.TabPages.Add(CreateTab("입출고 이력", _history));

        // 탭 전환 시 최신 데이터로 갱신
        tabs.SelectedIndexChanged += async (_, _) =>
        {
            switch (tabs.SelectedIndex)
            {
                case 0: await _dashboard.LoadDataAsync(); break;
                case 1: await _products.LoadDataAsync(); break;
                case 2: await _history.LoadDataAsync(); break;
            }
        };

        var status = new StatusStrip();
        status.Items.Add(new ToolStripStatusLabel("C# WinForms · ADO.NET · MS-SQL"));

        Controls.Add(tabs);
        Controls.Add(status);

        Load += async (_, _) => await _dashboard.LoadDataAsync();
    }

    private static TabPage CreateTab(string text, Control content)
    {
        var page = new TabPage(text) { Padding = new Padding(6) };
        page.Controls.Add(content);
        return page;
    }
}
