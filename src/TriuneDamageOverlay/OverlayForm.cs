using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Runtime.InteropServices;
using TriuneDamageOverlay.Core;

namespace TriuneDamageOverlay;

internal sealed class OverlayForm : Form
{
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private readonly List<FloatingDamage> _items = new();
    private readonly List<FloatingCombatAlert> _alerts = new();
    private readonly System.Windows.Forms.Timer _renderTimer = new() { Interval = 16 };
    private readonly AppSettings _settings;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool TextEnabled { get; set; } = true;

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            return cp;
        }
    }

    public OverlayForm(AppSettings settings)
    {
        _settings = settings;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        Bounds = SystemInformation.VirtualScreen;
        DoubleBuffered = true;
        _renderTimer.Tick += (_, _) => { ExpireItems(); Invalidate(); };
        _renderTimer.Start();
    }

    public void AddDamage(DamageEvent damage)
    {
        if (!TextEnabled) return;
        var text = _settings.ShowAbility && !string.IsNullOrWhiteSpace(damage.Ability)
            ? $"{damage.Amount:N0}  {FriendlyAbility(damage)}"
            : damage.Amount.ToString("N0");
        _items.Add(new FloatingDamage(text, damage.Kind, DateTime.UtcNow));
        if (_items.Count > 30) _items.RemoveAt(0);
    }

    public void AddCombatAlert(string text, Color color, int durationMilliseconds)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _alerts.Add(new FloatingCombatAlert(text, color, DateTime.UtcNow, Math.Clamp(durationMilliseconds, 500, 15000)));
        if (_alerts.Count > 4) _alerts.RemoveAt(0);
    }

    private static string FriendlyAbility(DamageEvent damage) => damage.Kind switch
    {
        DamageKind.Critical => "CRIT!",
        DamageKind.DamageOverTime => damage.Ability + " · DoT",
        DamageKind.Spell or DamageKind.Proc => damage.Ability,
        _ => damage.Ability.Length == 0 ? "Hit" : char.ToUpperInvariant(damage.Ability[0]) + damage.Ability[1..]
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        var now = DateTime.UtcNow;
        using var font = new Font("Segoe UI", _settings.FontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        var rowHeight = Math.Max(_settings.FontSize + 8, 30);
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index];
            var progress = Math.Clamp((now - item.Created).TotalMilliseconds / _settings.LifetimeMilliseconds, 0, 1);
            var alpha = (int)(255 * (1 - Math.Pow(progress, 2)));
            var rowsAboveNewest = _items.Count - 1 - index;
            var x = (float)(_settings.AnchorX - Bounds.Left);
            var ageSeconds = (now - item.Created).TotalSeconds;
            // Every hit keeps its own birth time. New hits occupy the bottom row,
            // older hits remain in chronological order and continuously scroll up.
            var y = _settings.AnchorY - Bounds.Top
                    - rowsAboveNewest * rowHeight
                    - (float)(ageSeconds * _settings.ScrollPixelsPerSecond);
            var baseColor = ColorFor(item.Kind);
            var color = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

            var size = e.Graphics.MeasureString(item.Text, font);
            x -= size.Width / 2;
            using var shadow = new SolidBrush(Color.FromArgb(Math.Min(alpha, 180), 0, 0, 0));
            using var brush = new SolidBrush(color);
            e.Graphics.DrawString(item.Text, font, shadow, x + 2, y + 2);
            e.Graphics.DrawString(item.Text, font, brush, x, y);
        }


        DrawCombatAlerts(e.Graphics, now);
    }

    private void DrawCombatAlerts(Graphics graphics, DateTime now)
    {
        var scale = Math.Clamp(_settings.CombatAlertScalePercent, 40, 200) / 100f;
        var alertFontSize = Math.Max(_settings.FontSize + 10, 38) * scale;
        var horizontalPadding = 14 * scale;
        var verticalPadding = 7 * scale;
        var rowGap = 18 * scale;
        using var alertFont = new Font("Segoe UI", alertFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        for (var index = 0; index < _alerts.Count; index++)
        {
            var alert = _alerts[index];
            var age = (now - alert.Created).TotalMilliseconds;
            var progress = Math.Clamp(age / alert.DurationMilliseconds, 0, 1);
            var fadeProgress = Math.Clamp((progress - .65) / .35, 0, 1);
            var alpha = (int)(255 * (1 - fadeProgress));
            var rowsAboveNewest = _alerts.Count - 1 - index;
            var size = graphics.MeasureString(alert.Text, alertFont);
            var x = _settings.AnchorX - Bounds.Left - size.Width / 2;
            var y = _settings.AnchorY - Bounds.Top - 150 - rowsAboveNewest * (alertFont.Height + rowGap);
            var box = new RectangleF(x - horizontalPadding, y - verticalPadding, size.Width + horizontalPadding * 2, size.Height + verticalPadding * 2);

            using var background = new SolidBrush(Color.FromArgb((int)(225 * (1 - fadeProgress)), 20, 20, 24));
            using var border = new Pen(Color.FromArgb(alpha, alert.Color.R, alert.Color.G, alert.Color.B), Math.Max(2, 3 * scale));
            using var textBrush = new SolidBrush(Color.FromArgb(alpha, alert.Color.R, alert.Color.G, alert.Color.B));
            graphics.FillRectangle(background, box);
            graphics.DrawRectangle(border, box.X, box.Y, box.Width, box.Height);
            graphics.DrawString(alert.Text, alertFont, textBrush, x, y);
        }
    }

    private void ExpireItems()
    {
        var cutoff = DateTime.UtcNow.AddMilliseconds(-_settings.LifetimeMilliseconds);
        _items.RemoveAll(x => x.Created < cutoff);
        var now = DateTime.UtcNow;
        _alerts.RemoveAll(x => (now - x.Created).TotalMilliseconds >= x.DurationMilliseconds);
    }

    internal static Color ColorFor(DamageKind kind) => kind switch
    {
        DamageKind.Critical => Color.FromArgb(255, 68, 68),
        DamageKind.Slashing => Color.FromArgb(255, 220, 90),
        DamageKind.Piercing => Color.FromArgb(78, 226, 210),
        DamageKind.Crushing => Color.FromArgb(255, 151, 72),
        DamageKind.Kick => Color.FromArgb(154, 230, 92),
        DamageKind.Bash => Color.FromArgb(255, 184, 112),
        DamageKind.Backstab => Color.FromArgb(255, 93, 177),
        DamageKind.Ranged => Color.FromArgb(117, 214, 116),
        DamageKind.Spell => Color.FromArgb(92, 208, 255),
        DamageKind.Proc => Color.FromArgb(83, 145, 255),
        DamageKind.DamageOverTime => Color.FromArgb(196, 126, 255),
        _ => Color.White
    };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _renderTimer.Stop();
        base.OnFormClosing(e);
    }

    private sealed record FloatingDamage(string Text, DamageKind Kind, DateTime Created);
    private sealed record FloatingCombatAlert(string Text, Color Color, DateTime Created, int DurationMilliseconds);
}
