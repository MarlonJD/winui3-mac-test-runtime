using System.Globalization;
using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

public sealed class SkiaV2SnapshotRenderer : ISnapshotRenderer
{
    public async Task<SnapshotResult> RenderAsync(
        UiTreeDocument tree,
        string screenshotsDirectory,
        SnapshotRenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var viewport = options?.Viewport ?? new VisualViewport(1280, 800);
        var scale = options?.Scale ?? 1.0;
        var width = Math.Max(1, (int)Math.Round(viewport.Width * scale));
        var height = Math.Max(1, (int)Math.Round(viewport.Height * scale));
        var theme = SkiaV2Theme.For(options?.Theme ?? "light");
        var fileName = options?.PreferredFileName ?? "mac-runtime.png";
        var path = Path.Combine(screenshotsDirectory, fileName);

        Directory.CreateDirectory(screenshotsDirectory);

        using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(theme.AppBackground);
        canvas.Scale((float)scale);

        using var typeface = SKTypeface.FromFamilyName("Segoe UI");
        using var titleFont = new SKFont(typeface, 22);
        using var bodyFont = new SKFont(typeface, 14);
        using var smallFont = new SKFont(typeface, 12);
        using var iconFont = new SKFont(typeface, 15);
        using var paint = new SKPaint { IsAntialias = true };

        RenderNode(canvas, tree.Root, theme, paint, titleFont, bodyFont, smallFont, iconFont, isRoot: true);

        cancellationToken.ThrowIfCancellationRequested();
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        await using var stream = File.Create(path);
        data.SaveTo(stream);

        return new SnapshotResult(ArtifactSchemas.SkiaV2Snapshot, "skia-v2-png", path, width, height, IsNonBlank: true);
    }

    private static void RenderNode(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont,
        bool isRoot = false)
    {
        if (node.Layout is null || node.Layout.Visibility == "Collapsed")
        {
            return;
        }

        switch (SimpleType(node))
        {
            case "Window":
                DrawRect(canvas, paint, Rect(node), theme.AppBackground);
                DrawRect(canvas, paint, new SKRect(0, 0, (float)node.Layout.Width, 48), theme.Surface);
                DrawLine(canvas, paint, 0, 48, (float)node.Layout.Width, 48, theme.Stroke);
                DrawText(canvas, paint, bodyFont, ReadString(node, "title") ?? "Window", 24, 31, theme.TextPrimary);
                RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "NavigationView":
                RenderNavigationView(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "Grid":
            case "Page":
            case "Frame":
            case "ScrollViewer":
            case "ContentControl":
            case "StackPanel":
                if (isRoot)
                {
                    DrawRect(canvas, paint, Rect(node), theme.AppBackground);
                }

                RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "Border":
                DrawRoundRect(canvas, paint, Rect(node), 8, theme.Surface);
                DrawRoundRectStroke(canvas, paint, Rect(node), 8, theme.Stroke);
                RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
                break;
            case "NavigationViewItem":
                RenderNavigationItem(canvas, node, theme, paint, bodyFont, iconFont);
                break;
            case "TextBlock":
                DrawText(canvas, paint, bodyFont, ReadText(node) ?? node.Name ?? string.Empty, (float)node.Layout.X, (float)node.Layout.Y + 19, theme.TextPrimary);
                break;
            case "CommandBar":
                RenderCommandBar(canvas, node, theme, paint, bodyFont, titleFont, smallFont, iconFont);
                break;
            case "AppBarButton":
                RenderAppBarButton(canvas, node, theme, paint, bodyFont, iconFont);
                break;
            case "ToggleButton":
                RenderToggleButton(canvas, node, theme, paint, bodyFont);
                break;
            case "CheckBox":
                RenderCheckBox(canvas, node, theme, paint, bodyFont);
                break;
            case "RadioButton":
                RenderRadioButton(canvas, node, theme, paint, bodyFont);
                break;
            case "Button":
                RenderButton(canvas, node, theme, paint, bodyFont);
                break;
            case "TextBox":
                RenderTextBox(canvas, node, theme, paint, bodyFont);
                break;
            case "ComboBox":
                RenderComboBox(canvas, node, theme, paint, bodyFont);
                break;
            case "ListView":
            case "ItemsControl":
                RenderListView(canvas, node, theme, paint, bodyFont, smallFont);
                break;
            case "ProgressRing":
                RenderProgressRing(canvas, node, theme, paint);
                break;
            case "ProgressBar":
                RenderProgressBar(canvas, node, theme, paint);
                break;
            case "InfoBar":
                RenderInfoBar(canvas, node, theme, paint, bodyFont, smallFont);
                break;
            case "FontIcon":
                DrawText(canvas, paint, iconFont, ReadString(node, "glyph") ?? "*", (float)node.Layout.X, (float)node.Layout.Y + 18, theme.Accent);
                break;
            case "Image":
                RenderImagePlaceholder(canvas, node, theme, paint, smallFont);
                break;
            case "String":
                DrawText(canvas, paint, bodyFont, ReadText(node) ?? string.Empty, (float)node.Layout.X, (float)node.Layout.Y + 19, theme.TextPrimary);
                break;
        }
    }

    private static void RenderNavigationView(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        var paneWidth = Math.Clamp(ReadFloat(node, "openPaneLength", 248), 220, 320);
        DrawRect(canvas, paint, new SKRect(rect.Left, rect.Top + 48, rect.Left + paneWidth, rect.Bottom), theme.PaneBackground);
        DrawLine(canvas, paint, rect.Left + paneWidth, rect.Top + 48, rect.Left + paneWidth, rect.Bottom, theme.Stroke);
        DrawText(canvas, paint, bodyFont, "Navigation", rect.Left + 20, rect.Top + 82, theme.TextSecondary);

        var selectedItem = ReadString(node, "selectedItem");
        foreach (var child in node.Children.Where(child => SimpleType(child) == "NavigationViewItem"))
        {
            RenderNavigationItem(canvas, child, theme, paint, bodyFont, iconFont, string.Equals(child.Name, selectedItem, StringComparison.Ordinal));
        }

        foreach (var child in node.Children.Where(child => SimpleType(child) != "NavigationViewItem"))
        {
            if (SimpleType(child) == "StackPanel")
            {
                RenderPaneFooter(canvas, child, theme, paint, bodyFont, smallFont);
            }
            else if (SimpleType(child) == "Frame")
            {
                RenderContentFrame(canvas, child, node, theme, paint, titleFont, bodyFont, smallFont);
            }
            else
            {
                RenderNode(canvas, child, theme, paint, titleFont, bodyFont, smallFont, iconFont);
            }
        }
    }

    private static void RenderNavigationItem(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont bodyFont,
        SKFont iconFont,
        bool selected = false)
    {
        if (node.Layout is null)
        {
            return;
        }

        var row = Rect(node);
        if (selected)
        {
            DrawRoundRect(canvas, paint, row, 8, theme.AccentSoft);
            DrawRoundRect(canvas, paint, new SKRect(row.Left, row.Top + 7, row.Left + 4, row.Bottom - 7), 2, theme.Accent);
        }

        DrawCircle(canvas, paint, row.Left + 14, row.Top + 20, 5, selected ? theme.Accent : theme.TextSecondary);
        DrawText(canvas, paint, bodyFont, ReadControlLabel(node, ToTitle(ReadString(node, "tag") ?? node.Name ?? "Item")), row.Left + 30, row.Top + 26, selected ? theme.Accent : theme.TextPrimary);
    }

    private static void RenderPaneFooter(
        SKCanvas canvas,
        UiNode footer,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont bodyFont,
        SKFont smallFont)
    {
        if (footer.Layout is null)
        {
            return;
        }

        var rect = Rect(footer);
        DrawLine(canvas, paint, rect.Left, rect.Top - 16, rect.Right, rect.Top - 16, theme.Stroke);

        var displayName = FindByName(footer, "AccountDisplayNameTextBlock");
        var username = FindByName(footer, "AccountUsernameTextBlock");
        var logout = FindByName(footer, "LogoutButton");
        var accountRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 74);
        DrawRoundRect(canvas, paint, accountRect, 10, theme.Surface);
        DrawRoundRect(canvas, paint, new SKRect(rect.Left + 12, rect.Top + 17, rect.Left + 48, rect.Top + 53), 18, theme.AccentSoft);
        DrawText(canvas, paint, bodyFont, ReadText(displayName) ?? "Demo Admin", rect.Left + 62, rect.Top + 31, theme.TextPrimary);
        DrawText(canvas, paint, smallFont, ReadText(username) ?? "@demo", rect.Left + 62, rect.Top + 52, theme.TextSecondary);

        if (logout is not null)
        {
            var button = new SKRect(rect.Left, rect.Top + 90, rect.Right, rect.Top + 130);
            DrawRoundRect(canvas, paint, button, 8, theme.Surface);
            DrawRoundRectStroke(canvas, paint, button, 8, theme.Stroke);
            DrawText(canvas, paint, bodyFont, ReadControlLabel(logout, "Sign out"), button.Left + 14, button.Top + 26, theme.TextPrimary);
        }
    }

    private static void RenderContentFrame(
        SKCanvas canvas,
        UiNode frame,
        UiNode navigationView,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont)
    {
        if (frame.Layout is null)
        {
            return;
        }

        var rect = Rect(frame);
        var title = ReadContentTitle(frame, navigationView);
        DrawText(canvas, paint, titleFont, title, rect.Left, rect.Top, theme.TextPrimary);
        DrawText(canvas, paint, bodyFont, "Reference visual scenario", rect.Left, rect.Top + 28, theme.TextSecondary);

        var card = new SKRect(rect.Left, rect.Top + 54, rect.Right, rect.Bottom);
        DrawRoundRect(canvas, paint, card, 10, theme.Surface);
        DrawRoundRectStroke(canvas, paint, card, 10, theme.Stroke);

        var y = card.Top + 42;
        var texts = Flatten(frame).Where(node => SimpleType(node) is "TextBlock" or "String").Take(8).ToArray();
        foreach (var text in texts)
        {
            DrawText(canvas, paint, bodyFont, ReadText(text) ?? text.Name ?? "Text", card.Left + 30, y, theme.TextPrimary);
            y += 28;
        }

        if (texts.Length == 0)
        {
            DrawText(canvas, paint, bodyFont, "Operational summary", card.Left + 30, y, theme.TextPrimary);
            DrawText(canvas, paint, smallFont, "Public fixture content rendered from the managed visual tree.", card.Left + 30, y + 26, theme.TextSecondary);
        }
    }

    private static void RenderButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var enabled = ReadBool(node, "isEnabled", fallback: true);
        DrawRoundRect(canvas, paint, rect, 6, enabled ? theme.Surface : theme.DisabledSurface);
        DrawRoundRectStroke(canvas, paint, rect, 6, theme.Stroke);
        DrawText(canvas, paint, font, ReadControlLabel(node, "Button"), rect.Left + 14, rect.Top + 25, enabled ? theme.TextPrimary : theme.TextDisabled);
    }

    private static void RenderAppBarButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font, SKFont iconFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, 6, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, 6, theme.Stroke);
        DrawText(canvas, paint, iconFont, "*", rect.Left + 12, rect.Top + 25, theme.Accent);
        DrawText(canvas, paint, font, ReadString(node, "label") ?? ReadControlLabel(node, "Command"), rect.Left + 30, rect.Top + 25, theme.TextPrimary);
    }

    private static void RenderToggleButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var checkedState = ReadBool(node, "isChecked", fallback: false);
        DrawRoundRect(canvas, paint, rect, 6, checkedState ? theme.AccentSoft : theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, 6, checkedState ? theme.Accent : theme.Stroke);
        DrawText(canvas, paint, font, ReadControlLabel(node, "Toggle"), rect.Left + 14, rect.Top + 25, checkedState ? theme.Accent : theme.TextPrimary);
    }

    private static void RenderCheckBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var checkedState = ReadBool(node, "isChecked", fallback: false);
        var box = new SKRect(rect.Left + 2, rect.Top + 9, rect.Left + 22, rect.Top + 29);
        DrawRoundRect(canvas, paint, box, 3, checkedState ? theme.Accent : theme.Surface);
        DrawRoundRectStroke(canvas, paint, box, 3, theme.Stroke);
        if (checkedState)
        {
            DrawText(canvas, paint, font, "x", box.Left + 6, box.Bottom - 4, theme.Surface);
        }

        DrawText(canvas, paint, font, ReadControlLabel(node, "Check box"), rect.Left + 32, rect.Top + 25, theme.TextPrimary);
    }

    private static void RenderRadioButton(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        var checkedState = ReadBool(node, "isChecked", fallback: false);
        DrawCircle(canvas, paint, rect.Left + 12, rect.Top + 20, 10, theme.Surface);
        DrawCircleStroke(canvas, paint, rect.Left + 12, rect.Top + 20, 10, theme.Stroke);
        if (checkedState)
        {
            DrawCircle(canvas, paint, rect.Left + 12, rect.Top + 20, 5, theme.Accent);
        }

        DrawText(canvas, paint, font, ReadControlLabel(node, "Option"), rect.Left + 32, rect.Top + 25, theme.TextPrimary);
    }

    private static void RenderTextBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, 4, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, 4, theme.Stroke);
        DrawText(canvas, paint, font, ReadText(node) ?? string.Empty, rect.Left + 10, rect.Top + 24, theme.TextPrimary);
    }

    private static void RenderComboBox(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont font)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, 4, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, 4, theme.Stroke);
        var text = ReadString(node, "selectedItem") ?? ReadString(node, "placeholderText") ?? "Select";
        DrawText(canvas, paint, font, text, rect.Left + 10, rect.Top + 25, theme.TextPrimary);
        DrawText(canvas, paint, font, "v", rect.Right - 22, rect.Top + 25, theme.TextSecondary);
    }

    private static void RenderListView(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont, SKFont smallFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, 8, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, 8, theme.Stroke);
        foreach (var child in node.Children)
        {
            if (child.Layout is null)
            {
                continue;
            }

            DrawText(canvas, paint, bodyFont, ReadText(child) ?? child.Name ?? "Item", (float)child.Layout.X, (float)child.Layout.Y + 19, theme.TextPrimary);
            DrawLine(canvas, paint, rect.Left + 12, (float)child.Layout.Y + 31, rect.Right - 12, (float)child.Layout.Y + 31, theme.Stroke);
        }
    }

    private static void RenderImagePlaceholder(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont smallFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, 8, theme.AccentSoft);
        DrawRoundRectStroke(canvas, paint, rect, 8, theme.Stroke);
        DrawLine(canvas, paint, rect.Left + 10, rect.Bottom - 10, rect.Right - 10, rect.Top + 10, theme.Accent);
        DrawText(canvas, paint, smallFont, "Image", rect.Left + 14, rect.Top + 24, theme.TextSecondary);
    }

    private static void RenderProgressBar(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint)
    {
        var rect = Rect(node);
        var min = ReadFloat(node, "minimum", 0);
        var max = Math.Max(min + 1, ReadFloat(node, "maximum", 100));
        var value = Math.Clamp(ReadFloat(node, "value", min), min, max);
        var progress = (value - min) / (max - min);
        var track = new SKRect(rect.Left, rect.Top + 9, rect.Right, rect.Bottom - 9);
        DrawRoundRect(canvas, paint, track, 4, theme.DisabledSurface);
        DrawRoundRect(canvas, paint, new SKRect(track.Left, track.Top, track.Left + track.Width * progress, track.Bottom), 4, theme.Accent);
    }

    private static void RenderProgressRing(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint)
    {
        var rect = Rect(node);
        var radius = Math.Min(rect.Width, rect.Height) / 2 - 4;
        DrawCircleStroke(canvas, paint, rect.Left + rect.Width / 2, rect.Top + rect.Height / 2, radius, theme.Accent);
        DrawCircle(canvas, paint, rect.Left + rect.Width / 2 + radius, rect.Top + rect.Height / 2, 3, theme.Accent);
    }

    private static void RenderInfoBar(SKCanvas canvas, UiNode node, SkiaV2Theme theme, SKPaint paint, SKFont bodyFont, SKFont smallFont)
    {
        if (!ReadBool(node, "isOpen", fallback: true))
        {
            return;
        }

        var rect = Rect(node);
        var severity = ReadString(node, "severity") ?? "Informational";
        var accent = severity switch
        {
            "Success" => new SKColor(0x0f, 0x7b, 0x0f),
            "Warning" => new SKColor(0x9d, 0x5d, 0x00),
            "Error" => new SKColor(0xc4, 0x2b, 0x1c),
            _ => theme.Accent
        };
        DrawRoundRect(canvas, paint, rect, 8, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, 8, theme.Stroke);
        DrawRoundRect(canvas, paint, new SKRect(rect.Left, rect.Top, rect.Left + 5, rect.Bottom), 2, accent);
        DrawText(canvas, paint, bodyFont, ReadString(node, "title") ?? severity, rect.Left + 18, rect.Top + 27, theme.TextPrimary);
        DrawText(canvas, paint, smallFont, ReadString(node, "message") ?? string.Empty, rect.Left + 18, rect.Top + 50, theme.TextSecondary);
    }

    private static void RenderCommandBar(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont bodyFont,
        SKFont titleFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        var rect = Rect(node);
        DrawRoundRect(canvas, paint, rect, 8, theme.Surface);
        DrawRoundRectStroke(canvas, paint, rect, 8, theme.Stroke);
        RenderChildren(canvas, node, theme, paint, titleFont, bodyFont, smallFont, iconFont);
    }

    private static void RenderChildren(
        SKCanvas canvas,
        UiNode node,
        SkiaV2Theme theme,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        SKFont iconFont)
    {
        foreach (var child in node.Children)
        {
            RenderNode(canvas, child, theme, paint, titleFont, bodyFont, smallFont, iconFont);
        }
    }

    private static string ReadContentTitle(UiNode frame, UiNode navigationView)
    {
        var firstText = Flatten(frame).FirstOrDefault(node => SimpleType(node) == "TextBlock");
        if (!string.IsNullOrWhiteSpace(ReadText(firstText)))
        {
            return ReadText(firstText)!;
        }

        var selectedItem = ReadString(navigationView, "selectedItem");
        var selected = navigationView.Children.FirstOrDefault(child => child.Name == selectedItem);
        return selected is null ? "Content" : ReadControlLabel(selected, ToTitle(ReadString(selected, "tag") ?? "Content"));
    }

    private static string ReadControlLabel(UiNode item, string fallback)
    {
        return ReadString(item, "automationName") ??
            ReadString(item, "content") ??
            ReadText(item) ??
            fallback;
    }

    private static IEnumerable<UiNode> Flatten(UiNode root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var nested in Flatten(child))
            {
                yield return nested;
            }
        }
    }

    private static UiNode? FindByName(UiNode root, string name)
    {
        return Flatten(root).FirstOrDefault(node => string.Equals(node.Name, name, StringComparison.Ordinal));
    }

    private static string SimpleType(UiNode node)
    {
        var type = node.Type;
        var dot = type.LastIndexOf('.');
        return dot < 0 ? type : type[(dot + 1)..];
    }

    private static SKRect Rect(UiNode node)
    {
        var layout = node.Layout ?? throw new InvalidOperationException($"Node '{node.Name ?? node.Type}' does not have layout.");
        return new SKRect(
            (float)layout.X,
            (float)layout.Y,
            (float)(layout.X + layout.Width),
            (float)(layout.Y + layout.Height));
    }

    private static string? ReadText(UiNode? node)
    {
        return node is null ? null : ReadString(node, "text");
    }

    private static string? ReadString(UiNode node, string key)
    {
        return node.Properties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool ReadBool(UiNode node, string key, bool fallback)
    {
        return node.Properties.TryGetValue(key, out var value) && bool.TryParse(value?.ToString(), out var boolean)
            ? boolean
            : fallback;
    }

    private static float ReadFloat(UiNode node, string key, float fallback)
    {
        if (!node.Properties.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : fallback;
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Item";
        }

        var normalized = value.Replace("-", " ", StringComparison.Ordinal);
        return string.Join(
            " ",
            normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part =>
                char.ToUpperInvariant(part[0]) + (part.Length == 1 ? string.Empty : part[1..])));
    }

    private static void DrawRect(SKCanvas canvas, SKPaint paint, SKRect rect, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawRect(rect, paint);
    }

    private static void DrawRoundRect(SKCanvas canvas, SKPaint paint, SKRect rect, float radius, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawRoundRect(rect, radius, radius, paint);
    }

    private static void DrawRoundRectStroke(SKCanvas canvas, SKPaint paint, SKRect rect, float radius, SKColor color)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1;
        paint.Color = color;
        canvas.DrawRoundRect(rect, radius, radius, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawLine(SKCanvas canvas, SKPaint paint, float x1, float y1, float x2, float y2, SKColor color)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1;
        paint.Color = color;
        canvas.DrawLine(x1, y1, x2, y2, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawCircle(SKCanvas canvas, SKPaint paint, float x, float y, float radius, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawCircle(x, y, radius, paint);
    }

    private static void DrawCircleStroke(SKCanvas canvas, SKPaint paint, float x, float y, float radius, SKColor color)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2;
        paint.Color = color;
        canvas.DrawCircle(x, y, radius, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawText(SKCanvas canvas, SKPaint paint, SKFont font, string text, float x, float y, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private sealed record SkiaV2Theme(
        SKColor AppBackground,
        SKColor PaneBackground,
        SKColor Surface,
        SKColor DisabledSurface,
        SKColor Stroke,
        SKColor TextPrimary,
        SKColor TextSecondary,
        SKColor TextDisabled,
        SKColor Accent,
        SKColor AccentSoft)
    {
        public static SkiaV2Theme For(string theme)
        {
            if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
            {
                return new SkiaV2Theme(
                    AppBackground: new SKColor(0x20, 0x22, 0x26),
                    PaneBackground: new SKColor(0x2a, 0x2d, 0x33),
                    Surface: new SKColor(0x31, 0x34, 0x3a),
                    DisabledSurface: new SKColor(0x3a, 0x3d, 0x43),
                    Stroke: new SKColor(0x4b, 0x50, 0x59),
                    TextPrimary: new SKColor(0xf5, 0xf6, 0xf8),
                    TextSecondary: new SKColor(0xbd, 0xc3, 0xcd),
                    TextDisabled: new SKColor(0x8d, 0x94, 0xa0),
                    Accent: new SKColor(0x74, 0xa7, 0xff),
                    AccentSoft: new SKColor(0x2f, 0x44, 0x69));
            }

            return new SkiaV2Theme(
                AppBackground: new SKColor(0xf7, 0xf8, 0xfa),
                PaneBackground: new SKColor(0xf2, 0xf3, 0xf5),
                Surface: new SKColor(0xff, 0xff, 0xff),
                DisabledSurface: new SKColor(0xed, 0xef, 0xf2),
                Stroke: new SKColor(0xd8, 0xdc, 0xe3),
                TextPrimary: new SKColor(0x1f, 0x23, 0x2a),
                TextSecondary: new SKColor(0x5d, 0x66, 0x73),
                TextDisabled: new SKColor(0x98, 0xa1, 0xad),
                Accent: new SKColor(0x25, 0x62, 0xd9),
                AccentSoft: new SKColor(0xe8, 0xf0, 0xff));
        }
    }
}
