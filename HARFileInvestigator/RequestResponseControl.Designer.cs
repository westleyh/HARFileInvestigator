using System.Drawing;
using System.Windows.Forms;

namespace HARFileInvestigator
{
    partial class RequestResponseControl
    {
        /// <summary>
        /// Designer created controls
        /// </summary>
        private TableLayoutPanel mainLayout;
        private FlowLayoutPanel actionPanel;
        private Button tearOutButton;
        private SplitContainer _split;
        private GroupBox requestGroupBox;
        private TableLayoutPanel requestLayout;
        private FlowLayoutPanel requestSearchPanel;
        private TextBox requestSearchTextBox;
        private Button requestPreviousButton;
        private Button requestSearchButton;
        private Button requestNextButton;
        private TabControl requestTabControl;
        private TabPage requestRawTabPage;
        private TabPage requestJwtTabPage;
        private TabPage requestJavaScriptStackTabPage;
        private RichTextBox requestTextBox;
        private RichTextBox requestJwtTextBox;
        private RichTextBox requestJavaScriptStackTextBox;

        private GroupBox responseGroupBox;
        private TableLayoutPanel responseLayout;
        private FlowLayoutPanel responseSearchPanel;
        private TextBox responseSearchTextBox;
        private Button responsePreviousButton;
        private Button responseSearchButton;
        private Button responseNextButton;
        private TabControl responseTabControl;
        private TabPage responseRawTabPage;
        private TabPage responseJwtTabPage;
        private RichTextBox responseTextBox;
        private RichTextBox responseJwtTextBox;

        private void InitializeComponent()
        {
            mainLayout = new TableLayoutPanel();
            actionPanel = new FlowLayoutPanel();
            tearOutButton = new Button();
            _split = new SplitContainer();
            requestGroupBox = new GroupBox();
            requestLayout = new TableLayoutPanel();
            requestSearchPanel = new FlowLayoutPanel();
            requestSearchTextBox = new TextBox();
            requestPreviousButton = new Button();
            requestSearchButton = new Button();
            requestNextButton = new Button();
            requestTabControl = new TabControl();
            requestRawTabPage = new TabPage();
            requestTextBox = new RichTextBox();
            requestJwtTabPage = new TabPage();
            requestJwtTextBox = new RichTextBox();
            requestJavaScriptStackTabPage = new TabPage();
            requestJavaScriptStackTextBox = new RichTextBox();
            responseGroupBox = new GroupBox();
            responseLayout = new TableLayoutPanel();
            responseSearchPanel = new FlowLayoutPanel();
            responseSearchTextBox = new TextBox();
            responsePreviousButton = new Button();
            responseSearchButton = new Button();
            responseNextButton = new Button();
            responseTabControl = new TabControl();
            responseRawTabPage = new TabPage();
            responseTextBox = new RichTextBox();
            responseJwtTabPage = new TabPage();
            responseJwtTextBox = new RichTextBox();
            mainLayout.SuspendLayout();
            actionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
            _split.Panel1.SuspendLayout();
            _split.Panel2.SuspendLayout();
            _split.SuspendLayout();
            requestGroupBox.SuspendLayout();
            requestLayout.SuspendLayout();
            requestSearchPanel.SuspendLayout();
            requestTabControl.SuspendLayout();
            requestRawTabPage.SuspendLayout();
            requestJwtTabPage.SuspendLayout();
            requestJavaScriptStackTabPage.SuspendLayout();
            responseGroupBox.SuspendLayout();
            responseLayout.SuspendLayout();
            responseSearchPanel.SuspendLayout();
            responseTabControl.SuspendLayout();
            responseRawTabPage.SuspendLayout();
            responseJwtTabPage.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(actionPanel, 0, 0);
            mainLayout.Controls.Add(_split, 0, 1);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 2;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(1682, 1427);
            mainLayout.TabIndex = 0;
            // 
            // actionPanel
            // 
            actionPanel.Controls.Add(tearOutButton);
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.FlowDirection = FlowDirection.RightToLeft;
            actionPanel.Location = new Point(3, 3);
            actionPanel.Name = "actionPanel";
            actionPanel.Padding = new Padding(4);
            actionPanel.Size = new Size(1676, 36);
            actionPanel.TabIndex = 0;
            actionPanel.WrapContents = false;
            // 
            // tearOutButton
            // 
            tearOutButton.AutoSize = true;
            tearOutButton.Location = new Point(1577, 7);
            tearOutButton.Name = "tearOutButton";
            tearOutButton.Size = new Size(88, 35);
            tearOutButton.TabIndex = 0;
            tearOutButton.Text = "Tear Out";
            tearOutButton.UseVisualStyleBackColor = true;
            // 
            // _split
            // 
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(3, 45);
            _split.Name = "_split";
            _split.Orientation = Orientation.Horizontal;
            // 
            // _split.Panel1
            // 
            _split.Panel1.Controls.Add(requestGroupBox);
            // 
            // _split.Panel2
            // 
            _split.Panel2.Controls.Add(responseGroupBox);
            _split.Size = new Size(1676, 1379);
            _split.SplitterDistance = 683;
            _split.TabIndex = 0;
            // 
            // requestGroupBox
            // 
            requestGroupBox.Controls.Add(requestLayout);
            requestGroupBox.Dock = DockStyle.Fill;
            requestGroupBox.Location = new Point(0, 0);
            requestGroupBox.Name = "requestGroupBox";
            requestGroupBox.Padding = new Padding(9, 10, 9, 10);
            requestGroupBox.Size = new Size(1676, 683);
            requestGroupBox.TabIndex = 0;
            requestGroupBox.TabStop = false;
            requestGroupBox.Text = "Request";
            // 
            // requestLayout
            // 
            requestLayout.ColumnCount = 1;
            requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            requestLayout.Controls.Add(requestSearchPanel, 0, 0);
            requestLayout.Controls.Add(requestTabControl, 0, 1);
            requestLayout.Dock = DockStyle.Fill;
            requestLayout.Location = new Point(9, 34);
            requestLayout.Name = "requestLayout";
            requestLayout.RowCount = 2;
            requestLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            requestLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            requestLayout.Size = new Size(1658, 639);
            requestLayout.TabIndex = 0;
            // 
            // requestSearchPanel
            // 
            requestSearchPanel.Controls.Add(requestSearchTextBox);
            requestSearchPanel.Controls.Add(requestPreviousButton);
            requestSearchPanel.Controls.Add(requestSearchButton);
            requestSearchPanel.Controls.Add(requestNextButton);
            requestSearchPanel.Dock = DockStyle.Fill;
            requestSearchPanel.Location = new Point(3, 3);
            requestSearchPanel.MinimumSize = new Size(0, 40);
            requestSearchPanel.Name = "requestSearchPanel";
            requestSearchPanel.Padding = new Padding(0, 2, 0, 0);
            requestSearchPanel.Size = new Size(1652, 40);
            requestSearchPanel.TabIndex = 0;
            // 
            // requestSearchTextBox
            // 
            requestSearchTextBox.Location = new Point(0, 2);
            requestSearchTextBox.Margin = new Padding(0, 0, 4, 0);
            requestSearchTextBox.MinimumSize = new Size(500, 0);
            requestSearchTextBox.Name = "requestSearchTextBox";
            requestSearchTextBox.PlaceholderText = "Search request";
            requestSearchTextBox.Size = new Size(500, 31);
            requestSearchTextBox.TabIndex = 0;
            // 
            // requestPreviousButton
            // 
            requestPreviousButton.AutoSize = true;
            requestPreviousButton.Location = new Point(504, 2);
            requestPreviousButton.Margin = new Padding(0, 0, 4, 0);
            requestPreviousButton.Name = "requestPreviousButton";
            requestPreviousButton.Size = new Size(89, 35);
            requestPreviousButton.TabIndex = 1;
            requestPreviousButton.Text = "Previous";
            // 
            // requestSearchButton
            // 
            requestSearchButton.AutoSize = true;
            requestSearchButton.Location = new Point(597, 2);
            requestSearchButton.Margin = new Padding(0, 0, 4, 0);
            requestSearchButton.Name = "requestSearchButton";
            requestSearchButton.Size = new Size(75, 35);
            requestSearchButton.TabIndex = 2;
            requestSearchButton.Text = "Search";
            // 
            // requestNextButton
            // 
            requestNextButton.AutoSize = true;
            requestNextButton.Location = new Point(676, 2);
            requestNextButton.Margin = new Padding(0, 0, 4, 0);
            requestNextButton.Name = "requestNextButton";
            requestNextButton.Size = new Size(75, 35);
            requestNextButton.TabIndex = 3;
            requestNextButton.Text = "Next";
            // 
            // requestTabControl
            // 
            requestTabControl.Controls.Add(requestRawTabPage);
            requestTabControl.Controls.Add(requestJwtTabPage);
            requestTabControl.Controls.Add(requestJavaScriptStackTabPage);
            requestTabControl.Dock = DockStyle.Fill;
            requestTabControl.Location = new Point(3, 37);
            requestTabControl.Name = "requestTabControl";
            requestTabControl.SelectedIndex = 0;
            requestTabControl.Size = new Size(1652, 599);
            requestTabControl.TabIndex = 0;
            // 
            // requestRawTabPage
            // 
            requestRawTabPage.Controls.Add(requestTextBox);
            requestRawTabPage.Location = new Point(4, 34);
            requestRawTabPage.Name = "requestRawTabPage";
            requestRawTabPage.Padding = new Padding(9, 10, 9, 10);
            requestRawTabPage.Size = new Size(1644, 561);
            requestRawTabPage.TabIndex = 0;
            requestRawTabPage.Text = "Raw";
            // 
            // requestTextBox
            // 
            requestTextBox.Dock = DockStyle.Fill;
            requestTextBox.Font = new Font("Consolas", 9F);
            requestTextBox.Location = new Point(9, 10);
            requestTextBox.Name = "requestTextBox";
            requestTextBox.ReadOnly = true;
            requestTextBox.Size = new Size(1626, 541);
            requestTextBox.TabIndex = 0;
            requestTextBox.Text = "";
            requestTextBox.WordWrap = false;
            // 
            // requestJwtTabPage
            // 
            requestJwtTabPage.Controls.Add(requestJwtTextBox);
            requestJwtTabPage.Location = new Point(4, 34);
            requestJwtTabPage.Name = "requestJwtTabPage";
            requestJwtTabPage.Padding = new Padding(9, 10, 9, 10);
            requestJwtTabPage.Size = new Size(1644, 561);
            requestJwtTabPage.TabIndex = 1;
            requestJwtTabPage.Text = "JWT";
            // 
            // requestJwtTextBox
            // 
            requestJwtTextBox.Dock = DockStyle.Fill;
            requestJwtTextBox.Font = new Font("Consolas", 9F);
            requestJwtTextBox.Location = new Point(9, 10);
            requestJwtTextBox.Name = "requestJwtTextBox";
            requestJwtTextBox.ReadOnly = true;
            requestJwtTextBox.Size = new Size(1626, 541);
            requestJwtTextBox.TabIndex = 0;
            requestJwtTextBox.Text = "";
            requestJwtTextBox.WordWrap = false;
            // 
            // requestJavaScriptStackTabPage
            // 
            requestJavaScriptStackTabPage.Controls.Add(requestJavaScriptStackTextBox);
            requestJavaScriptStackTabPage.Location = new Point(4, 34);
            requestJavaScriptStackTabPage.Name = "requestJavaScriptStackTabPage";
            requestJavaScriptStackTabPage.Padding = new Padding(9, 10, 9, 10);
            requestJavaScriptStackTabPage.Size = new Size(1644, 561);
            requestJavaScriptStackTabPage.TabIndex = 2;
            requestJavaScriptStackTabPage.Text = "JavaScript Stack";
            requestJavaScriptStackTabPage.UseVisualStyleBackColor = true;
            // 
            // requestJavaScriptStackTextBox
            // 
            requestJavaScriptStackTextBox.Dock = DockStyle.Fill;
            requestJavaScriptStackTextBox.Font = new Font("Consolas", 9F);
            requestJavaScriptStackTextBox.Location = new Point(9, 10);
            requestJavaScriptStackTextBox.Name = "requestJavaScriptStackTextBox";
            requestJavaScriptStackTextBox.ReadOnly = true;
            requestJavaScriptStackTextBox.Size = new Size(1626, 541);
            requestJavaScriptStackTextBox.TabIndex = 0;
            requestJavaScriptStackTextBox.Text = "";
            requestJavaScriptStackTextBox.WordWrap = false;
            // 
            // responseGroupBox
            // 
            responseGroupBox.Controls.Add(responseLayout);
            responseGroupBox.Dock = DockStyle.Fill;
            responseGroupBox.Location = new Point(0, 0);
            responseGroupBox.Name = "responseGroupBox";
            responseGroupBox.Padding = new Padding(9, 10, 9, 10);
            responseGroupBox.Size = new Size(1676, 692);
            responseGroupBox.TabIndex = 0;
            responseGroupBox.TabStop = false;
            responseGroupBox.Text = "Response";
            // 
            // responseLayout
            // 
            responseLayout.ColumnCount = 1;
            responseLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            responseLayout.Controls.Add(responseSearchPanel, 0, 0);
            responseLayout.Controls.Add(responseTabControl, 0, 1);
            responseLayout.Dock = DockStyle.Fill;
            responseLayout.Location = new Point(9, 34);
            responseLayout.Name = "responseLayout";
            responseLayout.RowCount = 2;
            responseLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            responseLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            responseLayout.Size = new Size(1658, 648);
            responseLayout.TabIndex = 0;
            // 
            // responseSearchPanel
            // 
            responseSearchPanel.Controls.Add(responseSearchTextBox);
            responseSearchPanel.Controls.Add(responsePreviousButton);
            responseSearchPanel.Controls.Add(responseSearchButton);
            responseSearchPanel.Controls.Add(responseNextButton);
            responseSearchPanel.Dock = DockStyle.Fill;
            responseSearchPanel.Location = new Point(3, 3);
            responseSearchPanel.MinimumSize = new Size(0, 40);
            responseSearchPanel.Name = "responseSearchPanel";
            responseSearchPanel.Padding = new Padding(0, 2, 0, 0);
            responseSearchPanel.Size = new Size(1652, 40);
            responseSearchPanel.TabIndex = 0;
            // 
            // responseSearchTextBox
            // 
            responseSearchTextBox.Location = new Point(0, 2);
            responseSearchTextBox.Margin = new Padding(0, 0, 4, 0);
            responseSearchTextBox.MinimumSize = new Size(500, 0);
            responseSearchTextBox.Name = "responseSearchTextBox";
            responseSearchTextBox.PlaceholderText = "Search response";
            responseSearchTextBox.Size = new Size(500, 31);
            responseSearchTextBox.TabIndex = 0;
            // 
            // responsePreviousButton
            // 
            responsePreviousButton.AutoSize = true;
            responsePreviousButton.Location = new Point(504, 2);
            responsePreviousButton.Margin = new Padding(0, 0, 4, 0);
            responsePreviousButton.Name = "responsePreviousButton";
            responsePreviousButton.Size = new Size(89, 35);
            responsePreviousButton.TabIndex = 1;
            responsePreviousButton.Text = "Previous";
            // 
            // responseSearchButton
            // 
            responseSearchButton.AutoSize = true;
            responseSearchButton.Location = new Point(597, 2);
            responseSearchButton.Margin = new Padding(0, 0, 4, 0);
            responseSearchButton.Name = "responseSearchButton";
            responseSearchButton.Size = new Size(75, 35);
            responseSearchButton.TabIndex = 2;
            responseSearchButton.Text = "Search";
            // 
            // responseNextButton
            // 
            responseNextButton.AutoSize = true;
            responseNextButton.Location = new Point(676, 2);
            responseNextButton.Margin = new Padding(0, 0, 4, 0);
            responseNextButton.Name = "responseNextButton";
            responseNextButton.Size = new Size(75, 35);
            responseNextButton.TabIndex = 3;
            responseNextButton.Text = "Next";
            // 
            // responseTabControl
            // 
            responseTabControl.Controls.Add(responseRawTabPage);
            responseTabControl.Controls.Add(responseJwtTabPage);
            responseTabControl.Dock = DockStyle.Fill;
            responseTabControl.Location = new Point(3, 37);
            responseTabControl.Name = "responseTabControl";
            responseTabControl.SelectedIndex = 0;
            responseTabControl.Size = new Size(1652, 608);
            responseTabControl.TabIndex = 0;
            // 
            // responseRawTabPage
            // 
            responseRawTabPage.Controls.Add(responseTextBox);
            responseRawTabPage.Location = new Point(4, 34);
            responseRawTabPage.Name = "responseRawTabPage";
            responseRawTabPage.Padding = new Padding(9, 10, 9, 10);
            responseRawTabPage.Size = new Size(1644, 570);
            responseRawTabPage.TabIndex = 0;
            responseRawTabPage.Text = "Raw";
            // 
            // responseTextBox
            // 
            responseTextBox.Dock = DockStyle.Fill;
            responseTextBox.Font = new Font("Consolas", 9F);
            responseTextBox.Location = new Point(9, 10);
            responseTextBox.Name = "responseTextBox";
            responseTextBox.ReadOnly = true;
            responseTextBox.Size = new Size(1626, 550);
            responseTextBox.TabIndex = 0;
            responseTextBox.Text = "";
            responseTextBox.WordWrap = false;
            // 
            // responseJwtTabPage
            // 
            responseJwtTabPage.Controls.Add(responseJwtTextBox);
            responseJwtTabPage.Location = new Point(4, 34);
            responseJwtTabPage.Name = "responseJwtTabPage";
            responseJwtTabPage.Padding = new Padding(9, 10, 9, 10);
            responseJwtTabPage.Size = new Size(1644, 570);
            responseJwtTabPage.TabIndex = 1;
            responseJwtTabPage.Text = "JWT";
            // 
            // responseJwtTextBox
            // 
            responseJwtTextBox.Dock = DockStyle.Fill;
            responseJwtTextBox.Font = new Font("Consolas", 9F);
            responseJwtTextBox.Location = new Point(9, 10);
            responseJwtTextBox.Name = "responseJwtTextBox";
            responseJwtTextBox.ReadOnly = true;
            responseJwtTextBox.Size = new Size(1626, 550);
            responseJwtTextBox.TabIndex = 0;
            responseJwtTextBox.Text = "";
            responseJwtTextBox.WordWrap = false;
            // 
            // RequestResponseControl
            // 
            Controls.Add(mainLayout);
            Name = "RequestResponseControl";
            Size = new Size(1682, 1427);
            mainLayout.ResumeLayout(false);
            actionPanel.ResumeLayout(false);
            actionPanel.PerformLayout();
            _split.Panel1.ResumeLayout(false);
            _split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_split).EndInit();
            _split.ResumeLayout(false);
            requestGroupBox.ResumeLayout(false);
            requestLayout.ResumeLayout(false);
            requestSearchPanel.ResumeLayout(false);
            requestSearchPanel.PerformLayout();
            requestTabControl.ResumeLayout(false);
            requestRawTabPage.ResumeLayout(false);
            requestJwtTabPage.ResumeLayout(false);
            requestJavaScriptStackTabPage.ResumeLayout(false);
            responseGroupBox.ResumeLayout(false);
            responseLayout.ResumeLayout(false);
            responseSearchPanel.ResumeLayout(false);
            responseSearchPanel.PerformLayout();
            responseTabControl.ResumeLayout(false);
            responseRawTabPage.ResumeLayout(false);
            responseJwtTabPage.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
