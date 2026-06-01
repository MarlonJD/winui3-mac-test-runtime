using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowsNativeProbe;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var options = ProbeOptions.Parse(args);
        ApplicationConfiguration.Initialize();
        Application.Run(new ProbeForm(options));
    }
}

internal sealed class ProbeForm : Form
{
    private readonly ProbeOptions options;

    public ProbeForm(ProbeOptions options)
    {
        this.options = options;
        Text = "WinUI3 Mac Test Runtime - " + options.ScenarioName;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(options.ViewportWidth, options.ViewportHeight);
        MinimumSize = new Size(Math.Min(960, options.ViewportWidth), Math.Min(640, options.ViewportHeight));
        BackColor = Palette.AppBackground;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (options.ScenarioName.Contains("interaction", StringComparison.OrdinalIgnoreCase))
        {
            DrawInteractionScenario(e.Graphics, ClientSize.Width, ClientSize.Height);
        }
        else
        {
            DrawShellScenario(e.Graphics, ClientSize.Width, ClientSize.Height);
        }
    }

    private static void DrawShellScenario(Graphics graphics, int width, int height)
    {
        using var bodyFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var smallFont = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        using var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
        Fill(graphics, Palette.AppBackground, new RectangleF(0, 0, width, height));
        Fill(graphics, Palette.Surface, new RectangleF(0, 0, width, 48));
        Line(graphics, Palette.Stroke, 0, 48, width, 48);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Sample Admin Shell", 24, 14);

        const int paneWidth = 248;
        Fill(graphics, Palette.PaneBackground, new RectangleF(0, 48, paneWidth, height - 48));
        Line(graphics, Palette.Stroke, paneWidth, 48, paneWidth, height);
        DrawText(graphics, bodyFont, Palette.TextSecondary, "Navigation", 20, 70);

        var items = new[] { "Home", "Channels", "Events", "Messages", "Notifications", "Settings", "Admin" };
        var y = 106;
        foreach (var item in items)
        {
            var selected = item == "Home";
            var row = new RectangleF(12, y, paneWidth - 24, 40);
            if (selected)
            {
                RoundRect(graphics, Palette.AccentSoft, row, 8);
                RoundRect(graphics, Palette.Accent, new RectangleF(row.Left, row.Top + 7, 4, row.Height - 14), 2);
            }

            FillEllipse(graphics, selected ? Palette.Accent : Palette.TextSecondary, row.Left + 9, row.Top + 15, 10, 10);
            DrawText(graphics, bodyFont, selected ? Palette.Accent : Palette.TextPrimary, item, row.Left + 30, row.Top + 10);
            y += 44;
        }

        var footerTop = height - 154;
        Line(graphics, Palette.Stroke, 14, footerTop - 16, paneWidth - 14, footerTop - 16);
        RoundRect(graphics, Palette.Surface, new RectangleF(14, footerTop, paneWidth - 28, 74), 10);
        RoundRect(graphics, Palette.AccentSoft, new RectangleF(26, footerTop + 17, 36, 36), 18);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Demo Admin", 76, footerTop + 18);
        DrawText(graphics, smallFont, Palette.TextSecondary, "@demo", 76, footerTop + 40);
        RoundRect(graphics, Palette.Surface, new RectangleF(14, footerTop + 90, paneWidth - 28, 40), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(14, footerTop + 90, paneWidth - 28, 40), 8);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Log out", 28, footerTop + 101);

        var contentX = paneWidth + 32;
        var contentY = 84;
        DrawText(graphics, titleFont, Palette.TextPrimary, "Home", contentX, contentY - 18);
        DrawText(graphics, bodyFont, Palette.TextSecondary, "Reference visual scenario", contentX, contentY + 9);
        var card = new RectangleF(contentX, contentY + 54, width - contentX - 32, height - contentY - 86);
        RoundRect(graphics, Palette.Surface, card, 10);
        StrokeRoundRect(graphics, Palette.Stroke, card, 10);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Operational summary", card.Left + 30, card.Top + 28);
        DrawText(graphics, smallFont, Palette.TextSecondary, "Public fixture content rendered from the Windows reference probe.", card.Left + 30, card.Top + 54);
    }

    private static void DrawInteractionScenario(Graphics graphics, int width, int height)
    {
        using var bodyFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
        Fill(graphics, Palette.AppBackground, new RectangleF(0, 0, width, height));
        Fill(graphics, Palette.Surface, new RectangleF(0, 0, width, 48));
        Line(graphics, Palette.Stroke, 0, 48, width, 48);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Interaction Binding App", 24, 14);

        var x = 24;
        var y = 76;
        DrawText(graphics, titleFont, Palette.TextPrimary, "Updated title", x, y);
        y += 36;
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width - 48, 36), 4);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width - 48, 36), 4);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Open tasks", x + 10, y + 9);
        y += 44;
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width - 48, 40), 6);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width - 48, 40), 6);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Done", x + 14, y + 11);
        y += 48;
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width - 48, 120), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width - 48, 120), 8);
        foreach (var item in new[] { "Review intake queue", "Confirm reviewer assignment", "Publish daily summary" })
        {
            DrawText(graphics, bodyFont, Palette.TextPrimary, item, x + 24, y + 16);
            Line(graphics, Palette.Stroke, x + 12, y + 42, width - 36, y + 42);
            y += 34;
        }
    }

    private static void Fill(Graphics graphics, Color color, RectangleF rectangle)
    {
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, rectangle);
    }

    private static void RoundRect(Graphics graphics, Color color, RectangleF rectangle, float radius)
    {
        using var brush = new SolidBrush(color);
        using var path = RoundedPath(rectangle, radius);
        graphics.FillPath(brush, path);
    }

    private static void StrokeRoundRect(Graphics graphics, Color color, RectangleF rectangle, float radius)
    {
        using var pen = new Pen(color);
        using var path = RoundedPath(rectangle, radius);
        graphics.DrawPath(pen, path);
    }

    private static void FillEllipse(Graphics graphics, Color color, float x, float y, float width, float height)
    {
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, x, y, width, height);
    }

    private static void Line(Graphics graphics, Color color, float x1, float y1, float x2, float y2)
    {
        using var pen = new Pen(color);
        graphics.DrawLine(pen, x1, y1, x2, y2);
    }

    private static void DrawText(Graphics graphics, Font font, Color color, string text, float x, float y)
    {
        using var brush = new SolidBrush(color);
        graphics.DrawString(text, font, brush, x, y);
    }

    private static GraphicsPath RoundedPath(RectangleF rectangle, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed record ProbeOptions(
    string ScenarioName,
    int ViewportWidth,
    int ViewportHeight)
{
    public static ProbeOptions Parse(string[] args)
    {
        string? scenarioPath = null;
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index] == "--scenario")
            {
                scenarioPath = args[index + 1];
            }
        }

        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            return new ProbeOptions("native-probe", 1280, 800);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var root = document.RootElement;
        var name = root.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString() ?? "scenario"
            : "scenario";
        var viewport = root.GetProperty("viewport");
        return new ProbeOptions(
            name,
            viewport.GetProperty("width").GetInt32(),
            viewport.GetProperty("height").GetInt32());
    }
}

internal static class Palette
{
    public static readonly Color AppBackground = Color.FromArgb(247, 248, 250);
    public static readonly Color PaneBackground = Color.FromArgb(242, 243, 245);
    public static readonly Color Surface = Color.White;
    public static readonly Color Stroke = Color.FromArgb(216, 220, 227);
    public static readonly Color TextPrimary = Color.FromArgb(31, 35, 42);
    public static readonly Color TextSecondary = Color.FromArgb(93, 102, 115);
    public static readonly Color Accent = Color.FromArgb(37, 98, 217);
    public static readonly Color AccentSoft = Color.FromArgb(232, 240, 255);
}
