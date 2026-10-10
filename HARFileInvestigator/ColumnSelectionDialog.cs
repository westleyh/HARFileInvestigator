using System.Drawing;
using System.Windows.Forms;

namespace HARFileInvestigator
{
    internal sealed class ColumnSelectionDialog : Form
    {
        private readonly TextBox _searchBox;
        private readonly ListView _columnsList;
        private readonly Button _applyButton;
        private readonly Dictionary<string, bool> _checkedColumns;
        private readonly IReadOnlyList<(string Name, string Header)> _columns;

        public IReadOnlySet<string> SelectedColumnNames { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

        public ColumnSelectionDialog(IReadOnlyList<(string Name, string Header)> columns, IReadOnlySet<string> visibleColumns)
        {
            _columns = columns;
            _checkedColumns = columns.ToDictionary(x => x.Name, x => visibleColumns.Contains(x.Name), StringComparer.Ordinal);

            Text = "Select Columns";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            MinimumSize = new Size(420, 420);
            ClientSize = new Size(560, 620);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            _searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Search columns"
            };
            _searchBox.TextChanged += (_, _) => RefreshColumnList();

            _columnsList = new ListView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                View = View.List,
                FullRowSelect = true,
                HideSelection = false
            };
            _columnsList.ItemChecked += (_, e) =>
            {
                if (e.Item.Tag is string name)
                {
                    _checkedColumns[name] = e.Item.Checked;
                }
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            _applyButton = new Button { Text = "Apply", AutoSize = true };
            var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
            _applyButton.Click += ApplyButton_Click;
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(_applyButton);

            layout.Controls.Add(_searchBox, 0, 0);
            layout.Controls.Add(_columnsList, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            Controls.Add(layout);

            AcceptButton = _applyButton;
            CancelButton = cancelButton;
            RefreshColumnList();
        }

        private void RefreshColumnList()
        {
            var filter = _searchBox.Text.Trim();
            _columnsList.BeginUpdate();
            try
            {
                _columnsList.Items.Clear();
                foreach (var column in _columns.Where(x =>
                             string.IsNullOrEmpty(filter) ||
                             x.Header.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                             x.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                {
                    var item = new ListViewItem(column.Header)
                    {
                        Tag = column.Name,
                        Checked = _checkedColumns[column.Name]
                    };
                    _columnsList.Items.Add(item);
                }
            }
            finally
            {
                _columnsList.EndUpdate();
            }
        }

        private void ApplyButton_Click(object? sender, EventArgs e)
        {
            var checkedColumns = _checkedColumns
                .Where(x => x.Value)
                .Select(x => x.Key)
                .ToHashSet(StringComparer.Ordinal);
            if (checkedColumns.Count == 0)
            {
                MessageBox.Show(this, "At least one column must remain visible.", "Select Columns", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedColumnNames = checkedColumns;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
