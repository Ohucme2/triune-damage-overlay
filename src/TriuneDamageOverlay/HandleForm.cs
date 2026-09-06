namespace TriuneDamageOverlay;

internal sealed class HandleForm : Form
{
    private readonly AppSettings _settings;
    private Point _dragOrigin;
    private Point _formOrigin;

    public event Action? ToggleRequested;
    public event Action? PositionChanged;

    public HandleForm(AppSettings settings)
    {
        _settings = settings;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(30, 30);
        BackColor = Color.FromArgb(35, 38, 48);
        Location = new Point(settings.AnchorX - Width / 2, settings.AnchorY - Height / 2);
        Cursor = Cursors.SizeAll;
        DoubleBuffered = true;
        new ToolTip().SetToolTip(this, AppInfo.DisplayName + " · Double-click to show or hide controls");
        MouseDoubleClick += (_, _) => ToggleRequested?.Invoke();
        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _dragOrigin = Cursor.Position; _formOrigin = Location; } };
        MouseMove += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            var delta = new Size(Cursor.Position.X - _dragOrigin.X, Cursor.Position.Y - _dragOrigin.Y);
            Location = _formOrigin + delta;
            _settings.AnchorX = Left + Width / 2;
            _settings.AnchorY = Top + Height / 2;
            PositionChanged?.Invoke();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var outer = new SolidBrush(Color.FromArgb(230, 26, 28, 36));
        using var inner = new SolidBrush(Color.FromArgb(255, 208, 63));
        e.Graphics.FillEllipse(outer, 1, 1, 28, 28);
        e.Graphics.FillEllipse(inner, 9, 9, 12, 12);
    }
}
