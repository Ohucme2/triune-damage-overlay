namespace TriuneDamageOverlay;

internal sealed class AlertHandleForm : Form
{
    private readonly AppSettings _settings;
    private Point _dragOrigin;
    private Point _formOrigin;

    public event Action? ToggleRequested;
    public event Action? PositionChanged;

    public AlertHandleForm(AppSettings settings)
    {
        _settings = settings;
        Text = "Move combat alerts";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(76, 30);
        BackColor = Color.FromArgb(35, 38, 48);
        ForeColor = Color.FromArgb(255, 126, 42);
        Location = new Point(settings.AlertAnchorX - Width / 2, settings.AlertAnchorY - Height / 2);
        Cursor = Cursors.SizeAll;
        DoubleBuffered = true;
        new ToolTip().SetToolTip(this, "Drag to position combat alerts · Double-click to show or hide controls");
        MouseDoubleClick += (_, _) => ToggleRequested?.Invoke();
        MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragOrigin = Cursor.Position;
            _formOrigin = Location;
        };
        MouseMove += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || _settings.PositionHandlesLocked) return;
            var delta = new Size(Cursor.Position.X - _dragOrigin.X, Cursor.Position.Y - _dragOrigin.Y);
            Location = _formOrigin + delta;
            _settings.AlertAnchorX = Left + Width / 2;
            _settings.AlertAnchorY = Top + Height / 2;
            PositionChanged?.Invoke();
        };
    }

    public void ApplyProfile()
    {
        Location = new Point(_settings.AlertAnchorX - Width / 2, _settings.AlertAnchorY - Height / 2);
        Cursor = _settings.PositionHandlesLocked ? Cursors.No : Cursors.SizeAll;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(Color.FromArgb(255, 126, 42), 2);
        using var text = new SolidBrush(Color.FromArgb(255, 190, 95));
        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        e.Graphics.DrawRectangle(border, 1, 1, Width - 3, Height - 3);
        var size = e.Graphics.MeasureString("ALERT", font);
        e.Graphics.DrawString("ALERT", font, text, (Width - size.Width) / 2, (Height - size.Height) / 2);
    }
}
