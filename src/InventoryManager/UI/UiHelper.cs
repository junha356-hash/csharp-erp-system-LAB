using Microsoft.Data.SqlClient;

namespace InventoryManager.UI;

public static class UiHelper
{
    public static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
        };
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 240, 248);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(204, 228, 247);
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.DataBindingComplete += (_, _) => grid.ClearSelection();
        return grid;
    }

    /// <summary>컬럼 헤더 텍스트/포맷 지정. 목록에 없는 컬럼은 숨긴다.</summary>
    public static void ConfigureColumns(DataGridView grid, params (string Name, string Header, string? Format)[] cols)
    {
        foreach (DataGridViewColumn c in grid.Columns) c.Visible = false;
        var order = 0;
        foreach (var (name, header, format) in cols)
        {
            var col = grid.Columns[name];
            if (col == null) continue;
            col.Visible = true;
            col.HeaderText = header;
            col.DisplayIndex = order++;
            if (format != null)
            {
                col.DefaultCellStyle.Format = format;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
    }

    /// <summary>DB 예외를 사용자 메시지로 변환해서 보여준다.</summary>
    public static void ShowError(Exception ex)
    {
        var message = ex switch
        {
            SqlException { Number: >= 50000 } se => se.Message,              // THROW 로 던진 업무 오류
            SqlException { Number: 2627 or 2601 } => "이미 존재하는 상품코드입니다.",
            SqlException { Number: 547 } => "제약 조건 위반입니다. 입력값을 확인하세요.",
            SqlException se => $"DB 오류가 발생했습니다.\n{se.Message}",
            _ => ex.Message,
        };
        MessageBox.Show(message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
