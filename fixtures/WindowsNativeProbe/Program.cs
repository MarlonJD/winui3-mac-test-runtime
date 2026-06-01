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
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = Point.Empty;
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

        if (options.ScenarioName.Contains("component-", StringComparison.OrdinalIgnoreCase))
        {
            DrawComponentLabScenario(e.Graphics, ClientSize.Width, ClientSize.Height, options.ScenarioName);
        }
        else if (options.ScenarioName.Contains("public-admin-workbench", StringComparison.OrdinalIgnoreCase))
        {
            DrawPublicAdminWorkbenchScenario(e.Graphics, ClientSize.Width, ClientSize.Height);
        }
        else if (options.ScenarioName.Contains("control-gallery", StringComparison.OrdinalIgnoreCase))
        {
            DrawControlGalleryScenario(e.Graphics, ClientSize.Width, ClientSize.Height);
        }
        else if (options.ScenarioName.Contains("interaction", StringComparison.OrdinalIgnoreCase))
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
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Closed tasks", x + 10, y + 9);
        y += 44;
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width - 48, 40), 6);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width - 48, 40), 6);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Done", x + 14, y + 11);
        y += 48;
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width - 48, 120), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width - 48, 120), 8);
        foreach (var item in new[] { "Review intake queue", "Confirm reviewer assignment", "Publish daily summary", "Archive completed task" })
        {
            DrawText(graphics, bodyFont, Palette.TextPrimary, item, x + 24, y + 16);
            Line(graphics, Palette.Stroke, x + 12, y + 42, width - 36, y + 42);
            y += 34;
        }
    }

    private static void DrawControlGalleryScenario(Graphics graphics, int width, int height)
    {
        using var bodyFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        Fill(graphics, Palette.AppBackground, new RectangleF(0, 0, width, height));
        Fill(graphics, Palette.Surface, new RectangleF(0, 0, width, 48));
        Line(graphics, Palette.Stroke, 0, 48, width, 48);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Public Control Gallery", 24, 14);

        var x = 24;
        var y = 76f;
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Control gallery", x, y + 4);
        y = 112;
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width - 48, 74), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width - 48, 74), 8);
        Fill(graphics, Palette.SuccessAccent, new RectangleF(x, y, 5, 74));
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Saved", x + 18, y + 18);
        DrawText(graphics, bodyFont, Palette.TextSecondary, "The public control gallery command ran.", x + 18, y + 42);

        y = 198;
        DrawField(graphics, bodyFont, "Operations", x, y, width - 48);
        y = 246;
        DrawField(graphics, bodyFont, "In review", x, y, width - 48);
        DrawText(graphics, bodyFont, Palette.TextSecondary, "v", width - 46, y + 11);
        y = 298;
        DrawCheckRow(graphics, bodyFont, "Enabled", x, y, check: true, radio: false);
        y = 350;
        DrawCheckRow(graphics, bodyFont, "High priority", x, y, check: true, radio: true);
        y = 402;
        RoundRect(graphics, Palette.AccentSoft, new RectangleF(x, y, width - 48, 40), 6);
        StrokeRoundRect(graphics, Palette.Accent, new RectangleF(x, y, width - 48, 40), 6);
        DrawText(graphics, bodyFont, Palette.Accent, "Pinned", x + 14, y + 14);

        y = 454;
        RoundRect(graphics, Palette.DisabledSurface, new RectangleF(x, y + 9, width - 48, 10), 4);
        RoundRect(graphics, Palette.Accent, new RectangleF(x, y + 9, (width - 48) * 0.65f, 10), 4);

        y = 494;
        StrokeRoundRect(graphics, Palette.Accent, new RectangleF((width / 2f) - 13, y + 3, 26, 26), 13);
        FillEllipse(graphics, Palette.Accent, (width / 2f) + 10, y + 14, 6, 6);

        y = 538;
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width - 48, 48), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width - 48, 48), 8);
        RoundRect(graphics, Palette.Surface, new RectangleF(x + 8, y + 6, 104, 36), 6);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x + 8, y + 6, 104, 36), 6);
        DrawText(graphics, bodyFont, Palette.Accent, "*", x + 20, y + 12);
        DrawText(graphics, bodyFont, Palette.Accent, "Save", x + 38, y + 13);

        DrawEmptyPanel(graphics, x, 598, width - 48, 64);
        DrawEmptyPanel(graphics, x, 674, width - 48, 64);
    }

    private static void DrawPublicAdminWorkbenchScenario(Graphics graphics, int width, int height)
    {
        using var bodyFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var smallFont = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        using var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
        Fill(graphics, Palette.AppBackground, new RectangleF(0, 0, width, height));
        Fill(graphics, Palette.Surface, new RectangleF(0, 0, width, 48));
        Line(graphics, Palette.Stroke, 0, 48, width, 48);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Public Admin Workbench", 24, 14);

        const int paneWidth = 248;
        Fill(graphics, Palette.PaneBackground, new RectangleF(0, 48, paneWidth, height - 48));
        Line(graphics, Palette.Stroke, paneWidth, 48, paneWidth, height);
        DrawText(graphics, bodyFont, Palette.TextSecondary, "Navigation", 20, 70);

        var items = new[] { "Overview", "Review queue", "Reports" };
        var y = 106;
        foreach (var item in items)
        {
            var selected = item == "Review queue";
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
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Public Admin", 76, footerTop + 18);
        DrawText(graphics, smallFont, Palette.TextSecondary, "public.fixture", 76, footerTop + 40);
        RoundRect(graphics, Palette.Surface, new RectangleF(14, footerTop + 90, paneWidth - 28, 40), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(14, footerTop + 90, paneWidth - 28, 40), 8);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Refresh", 28, footerTop + 101);

        var contentX = paneWidth + 32;
        var contentY = 76;
        DrawText(graphics, titleFont, Palette.TextPrimary, "Review queue", contentX, contentY);
        RoundRect(graphics, Palette.Surface, new RectangleF(contentX, contentY + 42, width - contentX - 32, 36), 4);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(contentX, contentY + 42, width - contentX - 32, 36), 4);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Filter by status or owner", contentX + 10, contentY + 51);

        var commandY = contentY + 92;
        RoundRect(graphics, Palette.Surface, new RectangleF(contentX, commandY, width - contentX - 32, 48), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(contentX, commandY, width - contentX - 32, 48), 8);
        RoundRect(graphics, Palette.Surface, new RectangleF(contentX + 8, commandY + 6, 104, 36), 6);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(contentX + 8, commandY + 6, 104, 36), 6);
        DrawText(graphics, bodyFont, Palette.Accent, "*", contentX + 20, commandY + 13);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Approve", contentX + 38, commandY + 13);
        RoundRect(graphics, Palette.Surface, new RectangleF(contentX + 120, commandY + 6, 104, 36), 6);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(contentX + 120, commandY + 6, 104, 36), 6);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Defer", contentX + 150, commandY + 13);

        var infoY = commandY + 62;
        RoundRect(graphics, Palette.Surface, new RectangleF(contentX, infoY, width - contentX - 32, 74), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(contentX, infoY, width - contentX - 32, 74), 8);
        Fill(graphics, Palette.SuccessAccent, new RectangleF(contentX, infoY, 5, 74));
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Approved", contentX + 18, infoY + 18);
        DrawText(graphics, smallFont, Palette.TextSecondary, "The selected public fixture request is approved.", contentX + 18, infoY + 44);

        var listY = infoY + 92;
        var listWidth = 320;
        RoundRect(graphics, Palette.Surface, new RectangleF(contentX, listY, listWidth, height - listY - 32), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(contentX, listY, listWidth, height - listY - 32), 8);
        var rows = new[] { "Access request from regional team", "Policy exception awaiting review", "Publishing approval for public notice" };
        y = listY + 18;
        foreach (var row in rows)
        {
            DrawText(graphics, bodyFont, Palette.TextPrimary, row, contentX + 18, y);
            Line(graphics, Palette.Stroke, contentX + 12, y + 28, contentX + listWidth - 12, y + 28);
            y += 44;
        }

        var detailX = contentX + listWidth + 24;
        RoundRect(graphics, Palette.Surface, new RectangleF(detailX, listY, width - detailX - 32, height - listY - 32), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(detailX, listY, width - detailX - 32, height - listY - 32), 8);
        DrawText(graphics, titleFont, Palette.TextPrimary, "Decision detail", detailX + 24, listY + 24);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Requester: Public Fixture User", detailX + 24, listY + 62);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Risk: Low", detailX + 24, listY + 94);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "State: Ready for review", detailX + 24, listY + 126);
        RoundRect(graphics, Palette.Surface, new RectangleF(detailX + 24, listY + 166, width - detailX - 80, 40), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(detailX + 24, listY + 166, width - detailX - 80, 40), 8);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Complete review", detailX + 38, listY + 177);
    }

    private static void DrawComponentLabScenario(Graphics graphics, int width, int height, string scenarioName)
    {
        using var bodyFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var smallFont = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        using var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
        Fill(graphics, Palette.AppBackground, new RectangleF(0, 0, width, height));
        Fill(graphics, Palette.Surface, new RectangleF(0, 0, width, 48));
        Line(graphics, Palette.Stroke, 0, 48, width, 48);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Component Parity Lab", 24, 14);

        const int paneWidth = 256;
        Fill(graphics, Palette.PaneBackground, new RectangleF(0, 48, paneWidth, height - 48));
        Line(graphics, Palette.Stroke, paneWidth, 48, paneWidth, height);
        DrawText(graphics, bodyFont, Palette.TextSecondary, "Navigation", 20, 70);

        var selectedTitle = ComponentLabTitle(scenarioName);
        var items = new[]
        {
            "Basic input",
            "Text and forms",
            "Collections",
            "Dialogs and flyouts",
            "Commands and menus",
            "Navigation",
            "Status and pickers",
            "Layout and media"
        };
        var y = 106;
        foreach (var item in items)
        {
            var selected = string.Equals(item, ComponentLabNavigationLabel(scenarioName), StringComparison.Ordinal);
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
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Public fixture", 28, footerTop + 18);
        RoundRect(graphics, Palette.Surface, new RectangleF(14, footerTop + 90, paneWidth - 28, 40), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(14, footerTop + 90, paneWidth - 28, 40), 8);
        DrawText(graphics, bodyFont, Palette.TextPrimary, "Reset page", 28, footerTop + 101);

        var contentX = paneWidth + 32;
        var contentY = 84;
        DrawText(graphics, titleFont, Palette.TextPrimary, selectedTitle, contentX, contentY - 18);
        DrawText(graphics, bodyFont, Palette.TextSecondary, "Reference visual scenario", contentX, contentY + 9);
        var card = new RectangleF(contentX, contentY + 54, width - contentX - 32, height - contentY - 86);
        RoundRect(graphics, Palette.Surface, card, 10);
        StrokeRoundRect(graphics, Palette.Stroke, card, 10);

        y = (int)card.Top + 42;
        foreach (var line in ComponentLabRows(scenarioName).Take(8))
        {
            DrawText(graphics, bodyFont, Palette.TextPrimary, line, card.Left + 30, y);
            y += 28;
        }

        DrawText(graphics, smallFont, Palette.TextSecondary, "Component-level grades are recorded in component-evidence.json.", card.Left + 30, card.Bottom - 34);
    }

    private static string ComponentLabNavigationLabel(string scenarioName)
    {
        if (scenarioName.Contains("text-forms", StringComparison.OrdinalIgnoreCase))
        {
            return "Text and forms";
        }

        if (scenarioName.Contains("collections", StringComparison.OrdinalIgnoreCase))
        {
            return "Collections";
        }

        if (scenarioName.Contains("dialogs-flyouts", StringComparison.OrdinalIgnoreCase))
        {
            return "Dialogs and flyouts";
        }

        if (scenarioName.Contains("commands-menus", StringComparison.OrdinalIgnoreCase))
        {
            return "Commands and menus";
        }

        if (scenarioName.Contains("navigation-workbench", StringComparison.OrdinalIgnoreCase))
        {
            return "Navigation";
        }

        if (scenarioName.Contains("status-pickers", StringComparison.OrdinalIgnoreCase))
        {
            return "Status and pickers";
        }

        return scenarioName.Contains("layout-media", StringComparison.OrdinalIgnoreCase)
            ? "Layout and media"
            : "Basic input";
    }

    private static string ComponentLabTitle(string scenarioName)
    {
        if (scenarioName.Contains("text-forms", StringComparison.OrdinalIgnoreCase))
        {
            return "Page 2: Text and forms";
        }

        if (scenarioName.Contains("collections", StringComparison.OrdinalIgnoreCase))
        {
            return "Page 3: Collections";
        }

        if (scenarioName.Contains("dialogs-flyouts", StringComparison.OrdinalIgnoreCase))
        {
            return "Page 4: Dialogs and flyouts";
        }

        if (scenarioName.Contains("commands-menus", StringComparison.OrdinalIgnoreCase))
        {
            return "Page 5: Commands and menus";
        }

        if (scenarioName.Contains("navigation-workbench", StringComparison.OrdinalIgnoreCase))
        {
            return "Page 6: Navigation and workbench";
        }

        if (scenarioName.Contains("status-pickers", StringComparison.OrdinalIgnoreCase))
        {
            return "Page 7: Status, progress, pickers, people";
        }

        return scenarioName.Contains("layout-media", StringComparison.OrdinalIgnoreCase)
            ? "Page 8: Layout, media, visuals"
            : "Page 1: Basic input";
    }

    private static IEnumerable<string> ComponentLabRows(string scenarioName)
    {
        if (scenarioName.Contains("text-forms", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Search label",
                "Updated public query",
                "RichTextBlock: planned diagnostic",
                "RichEditBox: planned diagnostic",
                "PasswordBox: planned diagnostic",
                "NumberBox: planned diagnostic",
                "AutoSuggestBox: planned diagnostic",
                "AutoSuggestBox.QueryIcon: planned diagnostic"
            };
        }

        if (scenarioName.Contains("collections", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Summary item one",
                "Summary item two",
                "Review intake",
                "Confirm owner",
                "DataTemplate: partial diagnostic",
                "ListView.ItemTemplate: partial diagnostic",
                "ItemsControl.ItemTemplate: partial diagnostic",
                "TreeView: planned diagnostic"
            };
        }

        if (scenarioName.Contains("dialogs-flyouts", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "ContentDialog: planned diagnostic",
                "Flyout: planned diagnostic",
                "TeachingTip: planned diagnostic",
                "ToolTip: planned diagnostic",
                "ToolTipService.SetToolTip: planned diagnostic"
            };
        }

        if (scenarioName.Contains("commands-menus", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Commands and menus",
                "Saved",
                "CommandBar.Content: partial diagnostic",
                "CommandBarFlyout: planned diagnostic",
                "MenuFlyout: planned diagnostic",
                "MenuBar: planned diagnostic",
                "Context menu pattern: planned diagnostic"
            };
        }

        if (scenarioName.Contains("navigation-workbench", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Overview",
                "Pane footer",
                "Queue item one",
                "Detail panel",
                "BreadcrumbBar: planned diagnostic",
                "Pivot: planned diagnostic",
                "SelectorBar: planned diagnostic",
                "TabView: planned diagnostic"
            };
        }

        if (scenarioName.Contains("status-pickers", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Complete",
                "The public lab status completed.",
                "InfoBadge: planned diagnostic",
                "PersonPicture: planned diagnostic",
                "ColorPicker: planned diagnostic",
                "CalendarDatePicker: planned diagnostic",
                "DatePicker: planned diagnostic",
                "TimePicker: planned diagnostic"
            };
        }

        if (scenarioName.Contains("layout-media", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Static resource resolved",
                "ThemeResource row",
                "SymbolIcon: planned diagnostic",
                "XamlControlsResources: fixture bootstrap diagnostic",
                "ResourceDictionary.ThemeDictionaries: planned diagnostic",
                "Color: resource diagnostic",
                "SolidColorBrush: resource diagnostic",
                "Window.SystemBackdrop and MicaBackdrop: planned diagnostic"
            };
        }

        return new[]
        {
            "Primary action ran",
            "Pinned",
            "Enabled",
            "High priority",
            "RepeatButton: planned diagnostic",
            "DropDownButton: planned diagnostic",
            "Slider: planned diagnostic",
            "RatingControl: planned diagnostic"
        };
    }

    private static void DrawField(Graphics graphics, Font font, string text, float x, float y, float width)
    {
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width, 38), 5);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width, 38), 5);
        DrawText(graphics, font, Palette.TextPrimary, text, x + 10, y + 10);
    }

    private static void DrawCheckRow(Graphics graphics, Font font, string text, float x, float y, bool check, bool radio)
    {
        if (radio)
        {
            StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x + 2, y + 9, 20, 20), 10);
            FillEllipse(graphics, check ? Palette.Accent : Palette.Surface, x + 8, y + 15, 8, 8);
        }
        else
        {
            RoundRect(graphics, check ? Palette.Accent : Palette.Surface, new RectangleF(x + 2, y + 9, 20, 20), 3);
            StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x + 2, y + 9, 20, 20), 3);
        }

        DrawText(graphics, font, Palette.TextPrimary, text, x + 34, y + 8);
    }

    private static void DrawEmptyPanel(Graphics graphics, float x, float y, float width, float height)
    {
        RoundRect(graphics, Palette.Surface, new RectangleF(x, y, width, height), 8);
        StrokeRoundRect(graphics, Palette.Stroke, new RectangleF(x, y, width, height), 8);
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
    public static readonly Color DisabledSurface = Color.FromArgb(239, 242, 247);
    public static readonly Color TextPrimary = Color.FromArgb(31, 35, 42);
    public static readonly Color TextSecondary = Color.FromArgb(93, 102, 115);
    public static readonly Color Accent = Color.FromArgb(37, 98, 217);
    public static readonly Color AccentSoft = Color.FromArgb(232, 240, 255);
    public static readonly Color SuccessAccent = Color.FromArgb(15, 123, 15);
}
