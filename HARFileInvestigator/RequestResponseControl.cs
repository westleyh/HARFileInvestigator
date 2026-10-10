using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HARFileInvestigator
{
    public partial class RequestResponseControl : UserControl
    {
        private Control? _originalParent;
        private int _originalIndex;
        private Form? _floatingForm;
        private Action? _onDockAction;
        private bool _dockedBack;
        private string _lastRequestSearchTerm = string.Empty;
        private string _lastResponseSearchTerm = string.Empty;
        private double _splitterRatio = 0.5;
        private bool _updatingSplitter;

        public event Action<bool>? TearOutStateChanged;

        public RequestResponseControl()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _split.SizeChanged += (_, _) => SetSplitterDistanceFromRatio();
            _split.SplitterMoved += (_, _) =>
            {
                if (!_updatingSplitter)
                {
                    var availableHeight = _split.ClientSize.Height - _split.SplitterWidth;
                    if (availableHeight > 0)
                    {
                        _splitterRatio = (double)_split.SplitterDistance / availableHeight;
                    }
                }
            };
            ForeColorChanged += (_, _) => UpdateSearchNavigationImages();
            ConfigureSearchNavigationButtons();
            UpdateSearchNavigationImages();
            SetSplitterDistanceFromRatio();

            tearOutButton.Click += (_, _) =>
            {
                if (IsTornOut)
                {
                    DockBack();
                }
                else
                {
                    TearOut(FindForm());
                }
            };

            requestSearchButton.Click += (_, _) => SearchPane(true, requestSearchTextBox.Text, true);
            requestNextButton.Click += (_, _) => SearchPane(true, requestSearchTextBox.Text, false);
            requestPreviousButton.Click += (_, _) => SearchPane(true, requestSearchTextBox.Text, false, false);
            requestSearchTextBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SearchPane(true, requestSearchTextBox.Text, false);
                    e.SuppressKeyPress = true;
                }
            };

            responseSearchButton.Click += (_, _) => SearchPane(false, responseSearchTextBox.Text, true);
            responseNextButton.Click += (_, _) => SearchPane(false, responseSearchTextBox.Text, false);
            responsePreviousButton.Click += (_, _) => SearchPane(false, responseSearchTextBox.Text, false, false);
            responseSearchTextBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SearchPane(false, responseSearchTextBox.Text, false);
                    e.SuppressKeyPress = true;
                }
            };
        }

        private void ConfigureSearchNavigationButtons()
        {
            ConfigureNavigationButton(requestPreviousButton, "Previous request match");
            ConfigureNavigationButton(requestNextButton, "Next request match");
            ConfigureNavigationButton(responsePreviousButton, "Previous response match");
            ConfigureNavigationButton(responseNextButton, "Next response match");
        }

        private static void ConfigureNavigationButton(Button button, string accessibleName)
        {
            button.AutoSize = false;
            button.Size = new Size(24, 24);
            button.Text = string.Empty;
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.AccessibleName = accessibleName;
            button.AccessibleDescription = accessibleName;
        }

        private void UpdateSearchNavigationImages()
        {
            SetNavigationImage(requestPreviousButton, up: true);
            SetNavigationImage(requestNextButton, up: false);
            SetNavigationImage(responsePreviousButton, up: true);
            SetNavigationImage(responseNextButton, up: false);
        }

        private void SetNavigationImage(Button button, bool up)
        {
            var previousImage = button.Image;
            button.Image = CreateChevronIcon(up, ForeColor);
            previousImage?.Dispose();
        }

        private static Bitmap CreateChevronIcon(bool up, Color color)
        {
            var bitmap = new Bitmap(14, 10);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            var points = up
                ? new[] { new PointF(2, 7), new PointF(7, 2), new PointF(12, 7) }
                : new[] { new PointF(2, 3), new PointF(7, 8), new PointF(12, 3) };
            graphics.DrawLines(pen, points);
            return bitmap;
        }

        private void SetSplitterDistanceFromRatio()
        {
            var availableHeight = _split.ClientSize.Height - _split.SplitterWidth;
            var minimumDistance = _split.Panel1MinSize;
            var maximumDistance = availableHeight - _split.Panel2MinSize;
            if (maximumDistance < minimumDistance)
            {
                return;
            }

            var distance = Math.Clamp((int)Math.Round(availableHeight * _splitterRatio), minimumDistance, maximumDistance);
            if (_split.SplitterDistance == distance)
            {
                return;
            }

            _updatingSplitter = true;
            try
            {
                _split.SplitterDistance = distance;
            }
            finally
            {
                _updatingSplitter = false;
            }
        }

        private void SearchPane(bool isRequest, string term, bool startFromBeginning, bool forward = true)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return;
            }

            var box = isRequest
                ? requestTabControl.SelectedTab == requestJwtTabPage ? requestJwtTextBox : requestTextBox
                : responseTabControl.SelectedTab == responseJwtTabPage ? responseJwtTextBox : responseTextBox;
            if (string.IsNullOrEmpty(box.Text))
            {
                return;
            }

            var lastTerm = isRequest ? _lastRequestSearchTerm : _lastResponseSearchTerm;
            var resetSearch = startFromBeginning || !string.Equals(lastTerm, term, StringComparison.OrdinalIgnoreCase);
            if (isRequest)
            {
                _lastRequestSearchTerm = term;
            }
            else
            {
                _lastResponseSearchTerm = term;
            }

            int index;
            if (forward)
            {
                var start = resetSearch ? 0 : Math.Min(box.Text.Length, box.SelectionStart + box.SelectionLength);
                index = box.Text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0 && start > 0)
                {
                    index = box.Text.IndexOf(term, 0, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                var start = resetSearch ? box.Text.Length - 1 : Math.Max(0, box.SelectionStart - 1);
                index = box.Text.LastIndexOf(term, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0 && start < box.Text.Length - 1)
                {
                    index = box.Text.LastIndexOf(term, box.Text.Length - 1, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (index < 0)
            {
                return;
            }

            box.Focus();
            box.Select(index, term.Length);
            box.ScrollToCaret();
        }

        // Expose inner controls for host integration
        public GroupBox RequestGroupBox => requestGroupBox;
        public GroupBox ResponseGroupBox => responseGroupBox;

        public TabControl RequestTabControl => requestTabControl;
        public TabPage RequestJwtTabPage => requestJwtTabPage;
        public TabPage RequestJavaScriptStackTabPage => requestJavaScriptStackTabPage;
        public RichTextBox RequestTextBox => requestTextBox;
        public RichTextBox RequestJwtTextBox => requestJwtTextBox;
        public RichTextBox RequestJavaScriptStackTextBox => requestJavaScriptStackTextBox;

        public TabControl ResponseTabControl => responseTabControl;
        public TabPage ResponseJwtTabPage => responseJwtTabPage;
        public RichTextBox ResponseTextBox => responseTextBox;
        public RichTextBox ResponseJwtTextBox => responseJwtTextBox;
        public string RequestSearchText => requestSearchTextBox.Text;
        public string ResponseSearchText => responseSearchTextBox.Text;
        public bool IsTornOut => _floatingForm is { IsDisposed: false };

        /// <summary>
        /// Tear out this control into a floating Form. Provide the owner form and an optional action
        /// that will be invoked when the control is docked back.
        /// </summary>
        public void TearOut(Form? owner = null, Action? onDock = null)
        {
            if (Parent is null || IsTornOut)
            {
                return;
            }

            if (Parent is Form)
            {
                return;
            }

            owner ??= FindForm();
            _originalParent = Parent;
            _originalIndex = _originalParent.Controls.GetChildIndex(this);
            _originalParent.Controls.Remove(this);

            _onDockAction = onDock;
            _dockedBack = false;

            _floatingForm = new TearOutDialog(owner, DockBack)
            {
                Text = "Request / Response",
                ClientSize = new Size(1000, 800),
                StartPosition = FormStartPosition.CenterParent
            };

            _floatingForm.FormClosed += FloatingForm_FormClosed;
            _floatingForm.Controls.Add(this);
            Dock = DockStyle.Fill;

            if (owner is null)
            {
                _floatingForm.Show();
            }
            else
            {
                _floatingForm.Show(owner);
            }

            tearOutButton.Text = "Dock Back";
            TearOutStateChanged?.Invoke(true);
        }

        public void DockBack()
        {
            var floatingForm = _floatingForm;
            if (floatingForm is null)
            {
                return;
            }

            _dockedBack = true;
            ReattachToOriginalParent();
            _floatingForm = null;
            floatingForm.Close();
            CompleteDockBack();
        }

        private void FloatingForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (_dockedBack)
            {
                _floatingForm = null;
                return;
            }

            _dockedBack = true;
            ReattachToOriginalParent();
            _floatingForm = null;
            CompleteDockBack();
        }

        private void CompleteDockBack()
        {
            tearOutButton.Text = "Tear Out";
            _onDockAction?.Invoke();
            _onDockAction = null;
            TearOutStateChanged?.Invoke(false);
        }

        private void ReattachToOriginalParent()
        {
            if (_originalParent is null)
            {
                return;
            }

            _floatingForm?.Controls.Remove(this);
            _originalParent.Controls.Add(this);
            _originalParent.Controls.SetChildIndex(this, Math.Min(_originalIndex, _originalParent.Controls.Count - 1));
            Dock = DockStyle.Fill;
        }

        private sealed class TearOutDialog : Form
        {
            private const int WmExitSizeMove = 0x0232;
            private readonly Form? _owner;
            private readonly Action _dockBack;

            public TearOutDialog(Form? owner, Action dockBack)
            {
                _owner = owner;
                _dockBack = dockBack;
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == WmExitSizeMove && _owner is { IsDisposed: false })
                {
                    var ownerClientBounds = _owner.RectangleToScreen(_owner.ClientRectangle);
                    if (ownerClientBounds.Contains(Cursor.Position))
                    {
                        _dockBack();
                    }
                }
            }
        }
    }
}
