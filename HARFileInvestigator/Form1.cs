
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HARFileInvestigator
{
    public partial class Form1 : Form
    {
        private const int MaxQueryHistory = 5;
        private readonly BindingList<HarTraceEntry> _filteredEntries = [];
        private readonly List<int> _matchRowIndices = [];
        private readonly Dictionary<string, DataGridViewColumn> _dynamicColumns = new(StringComparer.OrdinalIgnoreCase);
        private List<TagDefinition> _tagDefinitions = [];
        private List<HarTraceEntry> _allEntries = [];
        private bool _isFilterEnabled;
        private string? _currentHarFilePath;
        private readonly string _settingsFilePath;
        private AppUiSettings _settings = new();
        private int _currentMatchIndex = -1;
        private int _savedSessionSplitterDistance;
        private bool _hasSavedSessionSplitterDistance;
        private string? _cachedQueryText;
        private HarQueryExpression? _parsedQuery;
        private string? _queryParseError;
        private bool _hasCachedQueryParse;
        private bool _cachedQueryIsValid;
        private TimelineDialog? _timelineDialog;
        private readonly ContextMenuStrip _rowContextMenu = new();
        private readonly ToolStripMenuItem _rowTagMenuItem = new("Tag");

        private RequestResponseControl? requestResponseControl;

        public Form1()
        {
            InitializeComponent();

            requestResponseControl = new RequestResponseControl();
            requestResponseControl.TearOutStateChanged += RequestResponseControl_TearOutStateChanged;
            rightSplitContainer.Panel1.Controls.Clear();
            rightSplitContainer.Panel2.Controls.Clear();
            rightSplitContainer.Panel2Collapsed = true;
            rightSplitContainer.Panel1.Controls.Add(requestResponseControl);

            ConfigureFilterButtonVisual();
            ConfigureRowContextMenu();
            ConfigureQueryBoxShortcuts();
            InitializeGrid();

            entriesGrid.DataSource = _filteredEntries;

            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HARFileInvestigator",
                "ui-settings.json");

            LoadSettings();
            ApplyTheme(darkThemeCheckBox.Checked);
        }

        private void ConfigureQueryBoxShortcuts()
        {
            queryComboBox.KeyDown += QueryComboBox_KeyDown;
        }

        private void QueryComboBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            if (e.Modifiers == Keys.None || e.Modifiers == Keys.Control)
            {
                applyQueryButton_Click(this, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        }

        private void ConfigureFilterButtonVisual()
        {
            filterToggleButton.TextAlign = ContentAlignment.MiddleLeft;
            filterToggleButton.ImageAlign = ContentAlignment.MiddleLeft;
            filterToggleButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            filterToggleButton.Padding = new Padding(3, 0, 3, 0);
            var textHeight = TextRenderer.MeasureText("Filter", filterToggleButton.Font).Height;
            var iconSize = Math.Max(12, textHeight);
            filterToggleButton.Image = CreateFunnelIcon(iconSize);
            UpdateFilterToggleButtonText();
        }

        private static Bitmap CreateFunnelIcon(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(90, 90, 90));

            var w = size - 1f;
            var topY = Math.Max(1f, size * 0.12f);
            var midY = size * 0.5f;
            var stemTopX = size * 0.38f;
            var stemWidth = Math.Max(2f, size * 0.16f);
            var stemHeight = Math.Max(3f, size * 0.33f);

            var top = new[]
            {
                new PointF(1.5f, topY),
                new PointF(w - 1.5f, topY),
                new PointF(size * 0.62f, midY),
                new PointF(size * 0.38f, midY)
            };
            g.FillPolygon(brush, top);
            g.FillRectangle(brush, stemTopX, midY, stemWidth, stemHeight);

            return bitmap;
        }

        private void InitializeGrid()
        {
            entriesGrid.AutoGenerateColumns = false;
            entriesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            entriesGrid.ScrollBars = ScrollBars.Both;
            entriesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            entriesGrid.Columns.Clear();
            _dynamicColumns.Clear();
            entriesGrid.CellFormatting += EntriesGrid_CellFormatting;
            entriesGrid.ColumnHeaderMouseClick += EntriesGrid_ColumnHeaderMouseClick;

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.StartedDateTime),
                DataPropertyName = nameof(HarTraceEntry.StartedDateTime),
                HeaderText = "Started",
                Width = 210,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss.fff" }
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.Method),
                DataPropertyName = nameof(HarTraceEntry.Method),
                HeaderText = "Method",
                Width = 90
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.Host),
                DataPropertyName = nameof(HarTraceEntry.Host),
                HeaderText = "Host",
                Width = 260
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.Url),
                DataPropertyName = nameof(HarTraceEntry.Url),
                HeaderText = "URL",
                Width = 900
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.Status),
                DataPropertyName = nameof(HarTraceEntry.Status),
                HeaderText = "Status",
                Width = 90
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.MimeType),
                DataPropertyName = nameof(HarTraceEntry.MimeType),
                HeaderText = "MIME",
                Width = 220
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.DurationMs),
                DataPropertyName = nameof(HarTraceEntry.DurationMs),
                HeaderText = "Time (ms)",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.Tags),
                DataPropertyName = nameof(HarTraceEntry.Tags),
                HeaderText = "Tags",
                Width = 180
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(HarTraceEntry.JavaScriptStack),
                DataPropertyName = nameof(HarTraceEntry.JavaScriptStack),
                HeaderText = "JavaScript Stack",
                Width = 500,
                Visible = false
            });

        }

        private async void openButton_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "HAR files (*.har)|*.har|JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Open HAR Trace",
                InitialDirectory = Directory.Exists(_settings.LastOpenedDirectory) ? _settings.LastOpenedDirectory : string.Empty
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await LoadHarFileAsync(dialog.FileName);
        }

        private async Task LoadHarFileAsync(string fileName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                requestTextBox.Clear();
                responseTextBox.Clear();
                requestJwtTextBox.Clear();
                responseJwtTextBox.Clear();

                _allEntries = await HarTraceLoader.LoadAsync(fileName);
                _currentHarFilePath = fileName;
                RebuildDynamicColumns();
                ApplyFilters();

                _settings.LastOpenedFile = fileName;
                _settings.LastOpenedDirectory = Path.GetDirectoryName(fileName) ?? string.Empty;
                SaveSettings();

                Text = $"HAR File Investigator - {Path.GetFileName(fileName)} ({_allEntries.Count} entries)";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load HAR file:\n{ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void saveHarFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentHarFilePath) || !File.Exists(_currentHarFilePath))
            {
                MessageBox.Show(this, "No HAR file is loaded.", "Save HAR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "HAR files (*.har)|*.har|JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Save HAR File",
                FileName = Path.GetFileName(_currentHarFilePath),
                InitialDirectory = Path.GetDirectoryName(_currentHarFilePath) ?? string.Empty
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(_currentHarFilePath);
                var root = JsonNode.Parse(json)?.AsObject();
                var entries = root?["log"]?["entries"]?.AsArray();
                if (entries is null)
                {
                    throw new InvalidDataException("HAR log.entries array not found.");
                }

                var map = _allEntries.ToDictionary(x => x.EntryIndex, x => x.Tags);
                for (var i = 0; i < entries.Count; i++)
                {
                    if (entries[i] is not JsonObject entryObj)
                    {
                        continue;
                    }

                    if (!map.TryGetValue(i, out var tags) || string.IsNullOrWhiteSpace(tags))
                    {
                        entryObj.Remove("tag");
                    }
                    else
                    {
                        entryObj["tag"] = tags;
                    }
                }

                var output = root!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Save failed:\n{ex.Message}", "Save HAR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            if (_filteredEntries.Count == 0)
            {
                MessageBox.Show(this, "No entries to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Export Filtered Rows to CSV",
                FileName = "har-filtered.csv",
                InitialDirectory = Directory.Exists(_settings.LastOpenedDirectory) ? _settings.LastOpenedDirectory : string.Empty
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var csv = BuildCsv(_filteredEntries);
                File.WriteAllText(dialog.FileName, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                MessageBox.Show(this, $"Exported {_filteredEntries.Count} row(s).", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string BuildCsv(IEnumerable<HarTraceEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("StartedDateTime,Method,Host,Url,Status,StatusText,MimeType,DurationMs,IpAddress");

            foreach (var entry in entries)
            {
                sb.Append(Csv(entry.StartedDateTime.ToString("O", CultureInfo.InvariantCulture))).Append(',');
                sb.Append(Csv(entry.Method)).Append(',');
                sb.Append(Csv(entry.Host)).Append(',');
                sb.Append(Csv(entry.Url)).Append(',');
                sb.Append(Csv(entry.Status.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(Csv(entry.StatusText)).Append(',');
                sb.Append(Csv(entry.MimeType)).Append(',');
                sb.Append(Csv(entry.DurationMs.ToString("N1", CultureInfo.InvariantCulture))).Append(',');
                sb.Append(Csv(entry.IpAddress)).AppendLine();
            }

            return sb.ToString();
        }

        private static string Csv(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private void applyQueryButton_Click(object sender, EventArgs e)
        {
            var queryText = GetQueryText();
            if (!TryGetParsedQuery(queryText, out _, out var parseError))
            {
                MessageBox.Show(this, $"Invalid query: {parseError}", "Query", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ApplyFilters();
            AddQueryToHistory(queryText);
            SaveSettingsFromUi();
        }

        private void filterToggleButton_Click(object sender, EventArgs e)
        {
            _isFilterEnabled = !_isFilterEnabled;
            UpdateFilterToggleButtonText();
            ApplyFilters();
            SaveSettingsFromUi();
        }

        private void tagButton_Click(object sender, EventArgs e)
        {
            if (_matchRowIndices.Count == 0)
            {
                MessageBox.Show(this, "No highlighted items to tag.", "Tag", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var inputForm = new Form
            {
                Text = "Add Tag",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(420, 110)
            };

            var textBox = new TextBox { Left = 12, Top = 12, Width = 396 };
            var okButton = new Button { Text = "OK", Left = 252, Width = 75, Top = 50, DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "Cancel", Left = 333, Width = 75, Top = 50, DialogResult = DialogResult.Cancel };
            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(okButton);
            inputForm.Controls.Add(cancelButton);
            inputForm.AcceptButton = okButton;
            inputForm.CancelButton = cancelButton;

            if (inputForm.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var tag = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            var entries = _matchRowIndices
                .Where(i => i >= 0 && i < entriesGrid.Rows.Count)
                .Select(i => entriesGrid.Rows[i].DataBoundItem as HarTraceEntry)
                .Where(x => x is not null)
                .Cast<HarTraceEntry>()
                .ToList();

            ApplyTagToEntries(entries, tag);
            SaveSettingsFromUi();
        }

        private void tagMenuButton_Click(object sender, EventArgs e)
        {
            OpenTagManager();
        }

        private void OpenTagManager()
        {
            using var dialog = new TagManagerDialog(_tagDefinitions);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _tagDefinitions = dialog.Tags
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();

            entriesGrid.Refresh();
            SaveSettingsFromUi();
        }

        private void ApplyTagToEntries(IEnumerable<HarTraceEntry> entries, string tag)
        {
            EnsureTagDefinition(tag);

            foreach (var entry in entries)
            {
                var tags = entry.Tags
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (tags.All(x => !string.Equals(x, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    tags.Add(tag);
                    entry.Tags = string.Join(", ", tags);
                }
            }

            entriesGrid.Refresh();
        }

        private void clearQueryButton_Click(object sender, EventArgs e)
        {
            queryComboBox.Text = string.Empty;
            ApplyFilters();
            SaveSettingsFromUi();
        }

        private void previousMatchButton_Click(object sender, EventArgs e)
        {
            MoveToMatch(-1);
        }

        private void nextMatchButton_Click(object sender, EventArgs e)
        {
            MoveToMatch(1);
        }

        private void queryHistoryComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (queryComboBox.SelectedItem is string selectedQuery)
            {
                queryComboBox.Text = selectedQuery;
            }
        }

        private void timelineButton_Click(object sender, EventArgs e)
        {
            if (_timelineDialog is null || _timelineDialog.IsDisposed)
            {
                _timelineDialog = new TimelineDialog();
                _timelineDialog.ApplyTheme(darkThemeCheckBox.Checked);
                _timelineDialog.DotClicked += TimelineDialog_DotClicked;
                _timelineDialog.FormClosed += (_, _) => _timelineDialog = null;
            }

            if (!_timelineDialog.Visible)
            {
                _timelineDialog.Show(this);
            }
            else
            {
                _timelineDialog.BringToFront();
            }

            UpdateTimelineFromHighlights();
        }

        private void RequestResponseControl_TearOutStateChanged(bool isTornOut)
        {
            if (isTornOut)
            {
                if (splitContainer.Panel2Collapsed)
                {
                    return;
                }

                _savedSessionSplitterDistance = splitContainer.SplitterDistance;
                _hasSavedSessionSplitterDistance = true;
                splitContainer.Panel2Collapsed = true;
                return;
            }

            if (!splitContainer.Panel2Collapsed)
            {
                return;
            }

            splitContainer.Panel2Collapsed = false;
            if (_hasSavedSessionSplitterDistance)
            {
                var minDistance = splitContainer.Panel1MinSize;
                var maxDistance = splitContainer.Width - splitContainer.SplitterWidth - splitContainer.Panel2MinSize;
                if (maxDistance >= minDistance)
                {
                    splitContainer.SplitterDistance = Math.Clamp(_savedSessionSplitterDistance, minDistance, maxDistance);
                }
            }

            _hasSavedSessionSplitterDistance = false;
        }

        private void TimelineDialog_DotClicked(object? sender, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= entriesGrid.Rows.Count)
            {
                return;
            }

            entriesGrid.ClearSelection();
            entriesGrid.CurrentCell = entriesGrid.Rows[rowIndex].Cells[0];
            entriesGrid.Rows[rowIndex].Selected = true;

            var firstDisplayed = entriesGrid.FirstDisplayedScrollingRowIndex;
            if (firstDisplayed < 0)
            {
                return;
            }

            var displayedCount = entriesGrid.DisplayedRowCount(false);
            var lastDisplayed = firstDisplayed + displayedCount - 1;

            if (rowIndex < firstDisplayed || rowIndex > lastDisplayed)
            {
                entriesGrid.FirstDisplayedScrollingRowIndex = rowIndex;
            }
        }

        private void darkThemeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyTheme(darkThemeCheckBox.Checked);
            SaveSettingsFromUi();
        }

        private void ConfigureRowContextMenu()
        {
            _rowContextMenu.Opening += RowContextMenu_Opening;
            entriesGrid.ContextMenuStrip = _rowContextMenu;
        }

        private RichTextBox? GetActiveRequestSearchBox()
        {
            if (requestResponseControl is not null)
            {
                return requestResponseControl.RequestTabControl.SelectedTab == requestResponseControl.RequestJwtTabPage
                    ? requestResponseControl.RequestJwtTextBox
                    : requestResponseControl.RequestTabControl.SelectedTab == requestResponseControl.RequestJavaScriptStackTabPage
                        ? requestResponseControl.RequestJavaScriptStackTextBox
                        : requestResponseControl.RequestTextBox;
            }

            return requestTabControl.SelectedTab == requestJwtTabPage ? requestJwtTextBox : requestTextBox;
        }

        private RichTextBox? GetActiveResponseSearchBox()
        {
            if (requestResponseControl is not null)
            {
                return requestResponseControl.ResponseTabControl.SelectedTab == requestResponseControl.ResponseJwtTabPage
                    ? requestResponseControl.ResponseJwtTextBox
                    : requestResponseControl.ResponseTextBox;
            }

            return responseTabControl.SelectedTab == responseJwtTabPage ? responseJwtTextBox : responseTextBox;
        }

        private void RowContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            var clientPoint = entriesGrid.PointToClient(Cursor.Position);
            var hit = entriesGrid.HitTest(clientPoint.X, clientPoint.Y);
            if (hit.Type is not DataGridViewHitTestType.Cell and not DataGridViewHitTestType.RowHeader)
            {
                e.Cancel = true;
                return;
            }

            if (hit.RowIndex >= 0 && hit.RowIndex < entriesGrid.Rows.Count && !entriesGrid.Rows[hit.RowIndex].Selected)
            {
                entriesGrid.ClearSelection();
                entriesGrid.Rows[hit.RowIndex].Selected = true;
                if (entriesGrid.Rows[hit.RowIndex].Cells.Count > 0)
                {
                    entriesGrid.CurrentCell = entriesGrid.Rows[hit.RowIndex].Cells[0];
                }
            }

            _rowContextMenu.Items.Clear();

            _rowTagMenuItem.DropDownItems.Clear();
            foreach (var tag in _tagDefinitions.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var tagItem = new ToolStripMenuItem(tag.Name);
                tagItem.Click += (_, _) => ApplyTagToSelectedRows(tag.Name);
                _rowTagMenuItem.DropDownItems.Add(tagItem);
            }

            if (_rowTagMenuItem.DropDownItems.Count > 0)
            {
                _rowTagMenuItem.DropDownItems.Add(new ToolStripSeparator());
            }

            var manageItem = new ToolStripMenuItem("Tag manager...");
            manageItem.Click += (_, _) => OpenTagManager();
            _rowTagMenuItem.DropDownItems.Add(manageItem);

            var compareItem = new ToolStripMenuItem("Compare");
            compareItem.Click += (_, _) => CompareSelectedRows();

            var propertiesItem = new ToolStripMenuItem("Properties");
            propertiesItem.Click += (_, _) => ShowSelectedRowProperties();

            _rowContextMenu.Items.Add(_rowTagMenuItem);
            _rowContextMenu.Items.Add(compareItem);
            _rowContextMenu.Items.Add(propertiesItem);
        }

        private void ApplyTagToSelectedRows(string tag)
        {
            var selectedEntries = entriesGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(x => x.DataBoundItem as HarTraceEntry)
                .Where(x => x is not null)
                .Cast<HarTraceEntry>()
                .ToList();

            ApplyTagToEntries(selectedEntries, tag);
            SaveSettingsFromUi();
        }

        private void CompareSelectedRows()
        {
            var entries = entriesGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(x => x.DataBoundItem as HarTraceEntry)
                .Where(x => x is not null)
                .Cast<HarTraceEntry>()
                .Take(2)
                .ToList();

            if (entries.Count < 2)
            {
                MessageBox.Show(this, "Select at least two rows to compare.", "Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var left = entries[0];
            var right = entries[1];
            var sb = new StringBuilder();
            sb.AppendLine($"Method: {left.Method} | {right.Method}");
            sb.AppendLine($"Status: {left.Status} | {right.Status}");
            sb.AppendLine($"Host  : {left.Host} | {right.Host}");
            sb.AppendLine($"URL   : {left.Url} | {right.Url}");
            sb.AppendLine($"Time  : {left.DurationMs:N1} | {right.DurationMs:N1}");
            sb.AppendLine($"Tags  : {left.Tags} | {right.Tags}");

            MessageBox.Show(this, sb.ToString(), "Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowSelectedRowProperties()
        {
            if (entriesGrid.CurrentRow?.DataBoundItem is not HarTraceEntry entry)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Started: {entry.StartedDateTime:O}");
            sb.AppendLine($"Method: {entry.Method}");
            sb.AppendLine($"URL: {entry.Url}");
            sb.AppendLine($"Host: {entry.Host}");
            sb.AppendLine($"Status: {entry.Status} {entry.StatusText}");
            sb.AppendLine($"MIME: {entry.MimeType}");
            sb.AppendLine($"Duration: {entry.DurationMs:N1}");
            sb.AppendLine($"IP: {entry.IpAddress}");
            sb.AppendLine($"Tags: {entry.Tags}");

            foreach (var item in entry.AdditionalFields.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"{item.Key}: {item.Value}");
            }

            MessageBox.Show(this, sb.ToString(), "Properties", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EntriesGrid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var columns = entriesGrid.Columns
                .Cast<DataGridViewColumn>()
                .Select(x => (x.Name, x.HeaderText))
                .ToList();
            var visibleColumns = entriesGrid.Columns
                .Cast<DataGridViewColumn>()
                .Where(x => x.Visible)
                .Select(x => x.Name)
                .ToHashSet(StringComparer.Ordinal);

            using var dialog = new ColumnSelectionDialog(columns, visibleColumns);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            foreach (DataGridViewColumn column in entriesGrid.Columns)
            {
                column.Visible = dialog.SelectedColumnNames.Contains(column.Name);
            }
        }

        private void RebuildDynamicColumns()
        {
            foreach (var dynamicColumn in _dynamicColumns.Values)
            {
                if (entriesGrid.Columns.Contains(dynamicColumn))
                {
                    entriesGrid.Columns.Remove(dynamicColumn);
                }
            }

            _dynamicColumns.Clear();

            var fieldKeys = _allEntries
                .SelectMany(x => x.AdditionalFields.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var key in fieldKeys)
            {
                var column = new DataGridViewTextBoxColumn
                {
                    Name = $"Dyn::{key}",
                    HeaderText = key,
                    Width = 240,
                    Visible = false,
                    Tag = key
                };

                _dynamicColumns[key] = column;
                entriesGrid.Columns.Add(column);
            }

        }

        private void EntriesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var column = entriesGrid.Columns[e.ColumnIndex];
            if (entriesGrid.Rows[e.RowIndex].DataBoundItem is not HarTraceEntry entry)
            {
                return;
            }

            if (string.Equals(column.Name, nameof(HarTraceEntry.Tags), StringComparison.Ordinal))
            {
                ApplyTagCellColor(entry, e);
                return;
            }

            if (column.Tag is not string dynamicKey)
            {
                return;
            }

            e.Value = entry.GetAdditionalField(dynamicKey);
            e.FormattingApplied = true;
        }

        private void ApplyTagCellColor(HarTraceEntry entry, DataGridViewCellFormattingEventArgs e)
        {
            var firstTag = entry.Tags
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstTag))
            {
                return;
            }

            var tag = _tagDefinitions.FirstOrDefault(x => string.Equals(x.Name, firstTag, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                return;
            }

            var color = tag.Color;
            e.CellStyle.BackColor = color;
            e.CellStyle.ForeColor = GetContrastTextColor(color);
            e.CellStyle.SelectionBackColor = ControlPaint.Dark(color);
            e.CellStyle.SelectionForeColor = GetContrastTextColor(e.CellStyle.SelectionBackColor);
        }

        private void EnsureTagDefinition(string tag)
        {
            if (_tagDefinitions.Any(x => string.Equals(x.Name, tag, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            _tagDefinitions.Add(new TagDefinition
            {
                Name = tag,
                Color = GetDefaultTagColor(tag)
            });
        }

        private static Color GetDefaultTagColor(string key)
        {
            var hash = key.ToLowerInvariant().GetHashCode();
            var hue = Math.Abs(hash % 360);
            return ColorFromHsv(hue, 0.40, 0.95);
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            var v = Convert.ToInt32(value);
            var p = Convert.ToInt32(value * (1 - saturation));
            var q = Convert.ToInt32(value * (1 - f * saturation));
            var t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q)
            };
        }

        private static Color GetContrastTextColor(Color color)
        {
            var luminance = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
            return luminance > 140 ? Color.Black : Color.White;
        }

        private void ApplyFilters()
        {
            var queryText = GetQueryText();

            IEnumerable<HarTraceEntry> gridEntries = _allEntries;
            if (_isFilterEnabled && !string.IsNullOrWhiteSpace(queryText))
            {
                gridEntries = _allEntries.Where(x => MatchesQuery(x, queryText));
            }

            _filteredEntries.Clear();
            foreach (var entry in gridEntries)
            {
                _filteredEntries.Add(entry);
            }

            var matchedEntries = string.IsNullOrWhiteSpace(queryText)
                ? []
                : _filteredEntries.Where(x => MatchesQuery(x, queryText)).ToHashSet();

            UpdateMatchHighlights(matchedEntries);
            ApplyQueryHighlightsToPanes(queryText);
            UpdateStatusBarCounts();
        }

        private void ApplyQueryHighlightsToPanes(string queryText)
        {
            if (requestResponseControl is not null)
            {
                HighlightInPane(requestResponseControl.RequestTextBox, queryText);
                HighlightInPane(requestResponseControl.ResponseTextBox, queryText);
                HighlightInPane(requestResponseControl.RequestJwtTextBox, queryText);
                HighlightInPane(requestResponseControl.ResponseJwtTextBox, queryText);
                HighlightInPane(requestResponseControl.RequestJavaScriptStackTextBox, queryText);
            }
            else
            {
                HighlightInPane(requestTextBox, queryText);
                HighlightInPane(responseTextBox, queryText);
                HighlightInPane(requestJwtTextBox, queryText);
                HighlightInPane(responseJwtTextBox, queryText);
            }
        }

        private void HighlightInPane(RichTextBox box, string query)
        {
            var originalStart = box.SelectionStart;
            var originalLength = box.SelectionLength;

            box.SelectAll();
            box.SelectionBackColor = box.BackColor;
            box.SelectionColor = box.ForeColor;

            if (!string.IsNullOrWhiteSpace(query))
            {
                var index = 0;
                while ((index = box.Text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    box.Select(index, query.Length);
                    box.SelectionBackColor = Color.Yellow;
                    box.SelectionColor = Color.Black;
                    index += query.Length;
                }
            }

            box.Select(originalStart, originalLength);
        }

        private void UpdateFilterToggleButtonText()
        {
            var status = _isFilterEnabled ? "On" : "Off";
            filterToggleButton.Text = $"Filter ({status})";
            filterToggleButton.FlatStyle = _isFilterEnabled ? FlatStyle.Popup : FlatStyle.Standard;
            filterToggleButton.Font = new Font(filterToggleButton.Font, _isFilterEnabled ? FontStyle.Bold : FontStyle.Regular);
        }

        private void UpdateMatchHighlights(ISet<HarTraceEntry> matchedEntries)
        {
            _matchRowIndices.Clear();
            var matchBackColor = GetMatchHighlightColor();

            for (var i = 0; i < entriesGrid.Rows.Count; i++)
            {
                var row = entriesGrid.Rows[i];
                if (row.DataBoundItem is HarTraceEntry entry && matchedEntries.Contains(entry))
                {
                    row.DefaultCellStyle.BackColor = matchBackColor;
                    _matchRowIndices.Add(i);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.Empty;
                }
            }

            _currentMatchIndex = _matchRowIndices.Count > 0 ? 0 : -1;
            UpdateMatchNavigationButtons();

            if (_currentMatchIndex >= 0)
            {
                SelectCurrentMatch();
            }

            UpdateTimelineFromHighlights();
            ApplyQueryHighlightsToPanes(GetQueryText());
            AutoMarkQueryInPanesForHighlightedRow();
            UpdateStatusBarCounts();
        }

        private void AutoMarkQueryInPanesForHighlightedRow()
        {
            var query = GetQueryText();
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            var currentRowIndex = entriesGrid.CurrentRow?.Index ?? -1;
            if (currentRowIndex < 0 || !_matchRowIndices.Contains(currentRowIndex))
            {
                return;
            }

            if (requestResponseControl is not null && string.IsNullOrEmpty(requestResponseControl.RequestSearchText))
            {
                MarkFirstQueryOccurrence(GetActiveRequestSearchBox(), query);
            }

            if (requestResponseControl is not null && string.IsNullOrEmpty(requestResponseControl.ResponseSearchText))
            {
                MarkFirstQueryOccurrence(GetActiveResponseSearchBox(), query);
            }
        }

        private static void MarkFirstQueryOccurrence(RichTextBox? box, string query)
        {
            if (box is null || string.IsNullOrEmpty(box.Text))
            {
                return;
            }

            var index = box.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return;
            }

            box.SelectionStart = index;
            box.SelectionLength = query.Length;
            box.ScrollToCaret();
        }

        private Color GetMatchHighlightColor()
        {
            return darkThemeCheckBox.Checked
                ? Color.FromArgb(85, 85, 35)
                : Color.FromArgb(255, 247, 186);
        }

        private void MoveToMatch(int delta)
        {
            if (_matchRowIndices.Count == 0)
            {
                return;
            }

            _currentMatchIndex = (_currentMatchIndex + delta + _matchRowIndices.Count) % _matchRowIndices.Count;
            SelectCurrentMatch();
        }

        private void SelectCurrentMatch()
        {
            if (_currentMatchIndex < 0 || _currentMatchIndex >= _matchRowIndices.Count)
            {
                return;
            }

            var rowIndex = _matchRowIndices[_currentMatchIndex];
            if (rowIndex < 0 || rowIndex >= entriesGrid.Rows.Count)
            {
                return;
            }

            entriesGrid.ClearSelection();
            entriesGrid.CurrentCell = entriesGrid.Rows[rowIndex].Cells[0];
            entriesGrid.Rows[rowIndex].Selected = true;

            var firstDisplayed = entriesGrid.FirstDisplayedScrollingRowIndex;
            if (firstDisplayed < 0)
            {
                return;
            }

            var displayedCount = entriesGrid.DisplayedRowCount(false);
            var lastDisplayed = firstDisplayed + displayedCount - 1;

            if (rowIndex < firstDisplayed || rowIndex > lastDisplayed)
            {
                entriesGrid.FirstDisplayedScrollingRowIndex = rowIndex;
            }
        }

        private void UpdateMatchNavigationButtons()
        {
            var hasMatches = _matchRowIndices.Count > 0;
            previousMatchButton.Enabled = hasMatches;
            nextMatchButton.Enabled = hasMatches;
        }

        private bool MatchesQuery(HarTraceEntry entry, string queryText)
        {
            return TryGetParsedQuery(queryText, out var expression, out _) && expression!.Matches(entry);
        }

        private bool TryGetParsedQuery(string queryText, out HarQueryExpression? expression, out string? error)
        {
            if (!_hasCachedQueryParse || !string.Equals(_cachedQueryText, queryText, StringComparison.Ordinal))
            {
                _hasCachedQueryParse = true;
                _cachedQueryText = queryText;
                _cachedQueryIsValid = HarQueryParser.TryParse(queryText, out _parsedQuery, out _queryParseError);
            }

            expression = _parsedQuery;
            error = _queryParseError;
            return _cachedQueryIsValid;
        }

        private void entriesGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (entriesGrid.CurrentRow?.DataBoundItem is HarTraceEntry selected)
            {
                if (requestResponseControl is not null)
                {
                    requestResponseControl.RequestTextBox.Text = selected.RequestText;
                    requestResponseControl.RequestTextBox.SelectionStart = 0;
                    requestResponseControl.RequestTextBox.SelectionLength = 0;

                    requestResponseControl.ResponseTextBox.Text = selected.ResponseText;
                    requestResponseControl.ResponseTextBox.SelectionStart = 0;
                    requestResponseControl.ResponseTextBox.SelectionLength = 0;

                    requestResponseControl.RequestJwtTextBox.Text = BuildRequestJwtView(selected);
                    requestResponseControl.RequestJwtTextBox.SelectionStart = 0;
                    requestResponseControl.RequestJwtTextBox.SelectionLength = 0;

                    requestResponseControl.RequestJavaScriptStackTextBox.Text = selected.JavaScriptStack;
                    requestResponseControl.RequestJavaScriptStackTextBox.SelectionStart = 0;
                    requestResponseControl.RequestJavaScriptStackTextBox.SelectionLength = 0;

                    requestResponseControl.ResponseJwtTextBox.Text = BuildResponseJwtView(selected);
                    requestResponseControl.ResponseJwtTextBox.SelectionStart = 0;
                    requestResponseControl.ResponseJwtTextBox.SelectionLength = 0;
                }
                else
                {
                    requestTextBox.Text = selected.RequestText;
                    requestTextBox.SelectionStart = 0;
                    requestTextBox.SelectionLength = 0;

                    responseTextBox.Text = selected.ResponseText;
                    responseTextBox.SelectionStart = 0;
                    responseTextBox.SelectionLength = 0;

                    requestJwtTextBox.Text = BuildRequestJwtView(selected);
                    requestJwtTextBox.SelectionStart = 0;
                    requestJwtTextBox.SelectionLength = 0;

                    responseJwtTextBox.Text = BuildResponseJwtView(selected);
                    responseJwtTextBox.SelectionStart = 0;
                    responseJwtTextBox.SelectionLength = 0;
                }
            }
            else
            {
                if (requestResponseControl is not null)
                {
                    requestResponseControl.RequestTextBox.Clear();
                    requestResponseControl.ResponseTextBox.Clear();
                    requestResponseControl.RequestJwtTextBox.Clear();
                    requestResponseControl.RequestJavaScriptStackTextBox.Clear();
                    requestResponseControl.ResponseJwtTextBox.Clear();
                }
                else
                {
                    requestTextBox.Clear();
                    responseTextBox.Clear();
                    requestJwtTextBox.Clear();
                    responseJwtTextBox.Clear();
                }
            }

            UpdateTimelineFromHighlights();
            UpdateStatusBarCounts();
        }

        private void UpdateStatusBarCounts()
        {
            if (rowCountStatusLabel is null)
            {
                return;
            }

            var total = _filteredEntries.Count;
            var selected = entriesGrid?.SelectedRows?.Count ?? 0;
            var highlighted = _matchRowIndices.Count;

            rowCountStatusLabel.Text = $"Rows: {total} | Selected: {selected} | Highlighted: {highlighted}";
        }

        private void UpdateTimelineFromHighlights()
        {
            if (_timelineDialog is null || _timelineDialog.IsDisposed)
            {
                return;
            }

            var points = _matchRowIndices
                .Where(rowIndex => rowIndex >= 0 && rowIndex < entriesGrid.Rows.Count)
                .Select(rowIndex => new { rowIndex, row = entriesGrid.Rows[rowIndex] })
                .Select(x => new { x.rowIndex, entry = x.row.DataBoundItem as HarTraceEntry })
                .Where(x => x.entry is not null)
                .Select(x => new TimelineDialog.TimelinePoint(x.entry!.StartedDateTime, x.rowIndex, x.entry.Url))
                .ToList();

            var selectedRowIndices = entriesGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(x => x.Index)
                .ToList();

            _timelineDialog.UpdatePoints(points, GetMatchHighlightColor(), entriesGrid.SelectedRows.Count, selectedRowIndices);
        }

        private void ApplyTheme(bool dark)
        {
            if (_timelineDialog is not null && !_timelineDialog.IsDisposed)
            {
                _timelineDialog.ApplyTheme(dark);
            }

            if (!dark)
            {
                BackColor = SystemColors.Control;
                ForeColor = SystemColors.ControlText;
                ApplyControlTheme(this, SystemColors.Control, SystemColors.ControlText, SystemColors.Window, SystemColors.WindowText);
                entriesGrid.EnableHeadersVisualStyles = true;
                UpdateMatchHighlights(
                    string.IsNullOrWhiteSpace(GetQueryText())
                        ? []
                        : _allEntries.Where(x => MatchesQuery(x, GetQueryText())).ToHashSet());
                return;
            }

            var back = Color.FromArgb(30, 30, 30);
            var fore = Color.FromArgb(220, 220, 220);
            var surface = Color.FromArgb(45, 45, 45);

            BackColor = back;
            ForeColor = fore;
            ApplyControlTheme(this, back, fore, surface, fore);

            entriesGrid.BackgroundColor = surface;
            entriesGrid.GridColor = Color.FromArgb(70, 70, 70);
            entriesGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 55, 55);
            entriesGrid.ColumnHeadersDefaultCellStyle.ForeColor = fore;
            entriesGrid.EnableHeadersVisualStyles = false;
            entriesGrid.DefaultCellStyle.BackColor = surface;
            entriesGrid.DefaultCellStyle.ForeColor = fore;
            entriesGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            entriesGrid.DefaultCellStyle.SelectionForeColor = Color.White;

            UpdateMatchHighlights(
                string.IsNullOrWhiteSpace(GetQueryText())
                    ? []
                    : _allEntries.Where(x => MatchesQuery(x, GetQueryText())).ToHashSet());
        }

        private static void ApplyControlTheme(Control root, Color back, Color fore, Color inputBack, Color inputFore)
        {
            foreach (Control control in root.Controls)
            {
                switch (control)
                {
                    case TextBox:
                    case RichTextBox:
                    case ComboBox:
                        control.BackColor = inputBack;
                        control.ForeColor = inputFore;
                        break;
                    case DataGridView:
                        break;
                    default:
                        control.BackColor = back;
                        control.ForeColor = fore;
                        break;
                }

                ApplyControlTheme(control, back, fore, inputBack, inputFore);
            }
        }

        private static string BuildRequestJwtView(HarTraceEntry entry)
        {
            if (!TryExtractBearerToken(entry.RequestText, out var token))
            {
                return "No bearer token found in request Authorization header.";
            }

            return TryDecodeJwt(token, out var decoded)
                ? decoded
                : "Bearer token found, but it is not a valid JWT.";
        }

        private static string BuildResponseJwtView(HarTraceEntry entry)
        {
            if (!TryExtractAccessToken(entry.ResponseText, out var token))
            {
                return "No access token found in response body.";
            }

            return TryDecodeJwt(token, out var decoded)
                ? decoded
                : "Access token found, but it is not a valid JWT.";
        }

        private static bool TryExtractBearerToken(string requestText, out string token)
        {
            var match = Regex.Match(
                requestText,
                @"Authorization\s*:\s*Bearer\s+([A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+)",
                RegexOptions.IgnoreCase);

            token = match.Success ? match.Groups[1].Value : string.Empty;
            return match.Success;
        }

        private static bool TryExtractAccessToken(string responseText, out string token)
        {
            var bodyMatch = Regex.Match(responseText, @"Response Body:\s*(?<body>[\s\S]*)$", RegexOptions.IgnoreCase);
            var body = bodyMatch.Success ? bodyMatch.Groups["body"].Value : responseText;

            var jsonMatch = Regex.Match(
                body,
                "\"(?:access_token|accessToken|accesstoken)\"\\s*:\\s*\"([^\"\\r\\n]+)\"",
                RegexOptions.IgnoreCase);

            if (jsonMatch.Success)
            {
                token = jsonMatch.Groups[1].Value;
                return true;
            }

            var formMatch = Regex.Match(body, @"(?:^|[\?&\s])access_token=([^&\s]+)", RegexOptions.IgnoreCase);
            token = formMatch.Success ? Uri.UnescapeDataString(formMatch.Groups[1].Value) : string.Empty;
            return formMatch.Success;
        }

        private static bool TryDecodeJwt(string token, out string decoded)
        {
            decoded = string.Empty;

            var parts = token.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            if (!TryDecodeBase64Url(parts[0], out var headerJson) || !TryDecodeBase64Url(parts[1], out var payloadJson))
            {
                return false;
            }

            var formattedHeader = TryFormatJson(headerJson);
            var formattedPayload = TryFormatJson(payloadJson);

            var sb = new StringBuilder();
            sb.AppendLine("JWT");
            sb.AppendLine();
            sb.AppendLine("Header:");
            sb.AppendLine(formattedHeader);
            sb.AppendLine();
            sb.AppendLine("Payload:");
            sb.AppendLine(formattedPayload);

            decoded = sb.ToString();
            return true;
        }

        private static bool TryDecodeBase64Url(string input, out string decoded)
        {
            decoded = string.Empty;

            try
            {
                var base64 = input.Replace('-', '+').Replace('_', '/');
                var padding = 4 - (base64.Length % 4);
                if (padding is > 0 and < 4)
                {
                    base64 = base64.PadRight(base64.Length + padding, '=');
                }

                var bytes = Convert.FromBase64String(base64);
                decoded = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string TryFormatJson(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return raw;
            }
        }

        private void LoadSettings()
        {
            _settings = AppUiSettings.Load(_settingsFilePath);

            queryComboBox.Text = _settings.SearchFilter;
            RefreshQueryHistoryDropdown();
            darkThemeCheckBox.Checked = _settings.DarkTheme;
            _isFilterEnabled = _settings.FilterEnabled;
            _tagDefinitions = (_settings.TagDefinitions ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            UpdateFilterToggleButtonText();

            if (_settings.Width > 0 && _settings.Height > 0)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Math.Max(0, _settings.Left), Math.Max(0, _settings.Top));
                Size = new Size(_settings.Width, _settings.Height);
            }

            if (_settings.SplitterDistance > 0)
            {
                splitContainer.SplitterDistance = Math.Min(_settings.SplitterDistance, splitContainer.Width - 150);
            }

            if (_settings.DetailsSplitterDistance > 0)
            {
                rightSplitContainer.SplitterDistance = Math.Min(_settings.DetailsSplitterDistance, rightSplitContainer.Height - 120);
            }

            if (!string.IsNullOrWhiteSpace(_settings.LastOpenedFile) && File.Exists(_settings.LastOpenedFile))
            {
                _ = LoadHarFileAsync(_settings.LastOpenedFile);
            }
        }

        private void AddQueryToHistory(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            _settings.QueryHistory ??= [];
            _settings.QueryHistory.RemoveAll(x => string.Equals(x, query, StringComparison.OrdinalIgnoreCase));
            _settings.QueryHistory.Insert(0, query);

            if (_settings.QueryHistory.Count > MaxQueryHistory)
            {
                _settings.QueryHistory = _settings.QueryHistory.Take(MaxQueryHistory).ToList();
            }

            RefreshQueryHistoryDropdown();
        }

        private void RefreshQueryHistoryDropdown()
        {
            queryComboBox.Items.Clear();

            var currentText = queryComboBox.Text;

            if (_settings.QueryHistory is null)
            {
                queryComboBox.Text = currentText;
                return;
            }

            foreach (var query in _settings.QueryHistory)
            {
                queryComboBox.Items.Add(query);
            }

            queryComboBox.Text = currentText;
        }

        private void SaveSettingsFromUi()
        {
            _settings.SearchFilter = GetQueryText();
            _settings.DarkTheme = darkThemeCheckBox.Checked;
            _settings.FilterEnabled = _isFilterEnabled;
            _settings.TagDefinitions = _tagDefinitions
                .Select(x => new TagDefinition
                {
                    Name = x.Name,
                    ColorArgb = x.ColorArgb
                })
                .ToList();
            SaveSettings();
        }

        private string GetQueryText()
        {
            return queryComboBox.Text.Trim();
        }

        private void SaveSettings()
        {
            _settings.Left = Left;
            _settings.Top = Top;
            _settings.Width = Width;
            _settings.Height = Height;
            _settings.SplitterDistance = splitContainer.SplitterDistance;
            _settings.DetailsSplitterDistance = rightSplitContainer.SplitterDistance;
            AppUiSettings.Save(_settingsFilePath, _settings);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_timelineDialog is not null && !_timelineDialog.IsDisposed)
            {
                _timelineDialog.Close();
            }

            SaveSettingsFromUi();
        }
    }
}
