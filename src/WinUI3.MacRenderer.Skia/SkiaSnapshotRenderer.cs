using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

public sealed class SkiaSnapshotRenderer : ISnapshotRenderer
{
    private static readonly SKColor AppBackground = new(0xf7, 0xf8, 0xfa);
    private static readonly SKColor PaneBackground = new(0xf2, 0xf3, 0xf5);
    private static readonly SKColor Surface = new(0xff, 0xff, 0xff);
    private static readonly SKColor Stroke = new(0xd8, 0xdc, 0xe3);
    private static readonly SKColor TextPrimary = new(0x1f, 0x23, 0x2a);
    private static readonly SKColor TextSecondary = new(0x5d, 0x66, 0x73);
    private static readonly SKColor Accent = new(0x25, 0x62, 0xd9);
    private static readonly SKColor AccentSoft = new(0xe8, 0xf0, 0xff);

    public async Task<SnapshotResult> RenderAsync(
        UiTreeDocument tree,
        string screenshotsDirectory,
        SnapshotRenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tree);

        Directory.CreateDirectory(screenshotsDirectory);
        var path = Path.Combine(screenshotsDirectory, "snapshot.png");
        var size = FindFirst(tree.Root, IsNavigationView) is null
            ? MeasureDiagnosticSnapshot(tree.Root)
            : (Width: 1280, Height: 800);

        using var bitmap = new SKBitmap(size.Width, size.Height);
        using var canvas = new SKCanvas(bitmap);
        using var typeface = SKTypeface.FromFamilyName("SF Pro Text");
        using var titleFont = new SKFont(typeface, 20);
        using var bodyFont = new SKFont(typeface, 14);
        using var smallFont = new SKFont(typeface, 12);
        using var paint = new SKPaint { IsAntialias = true };

        if (FindFirst(tree.Root, IsNavigationView) is { } navigationView)
        {
            RenderNavigationShell(canvas, tree.Root, navigationView, paint, titleFont, bodyFont, smallFont, size.Width, size.Height);
        }
        else
        {
            RenderDiagnosticSnapshot(canvas, tree.Root, paint, titleFont, bodyFont, size.Width, size.Height);
        }

        var imageIntegrity = RuntimeImageIntegrityAnalyzer.Analyze(bitmap);

        cancellationToken.ThrowIfCancellationRequested();
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        await using var stream = File.Create(path);
        data.SaveTo(stream);

        return new SnapshotResult(
            ArtifactSchemas.Snapshot,
            "skia-png",
            path,
            size.Width,
            size.Height,
            IsNonBlank: imageIntegrity.IsNonBlank,
            RuntimeImageIntegrity: imageIntegrity);
    }

    private static void RenderNavigationShell(
        SKCanvas canvas,
        UiNode root,
        UiNode navigationView,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        int width,
        int height)
    {
        canvas.Clear(AppBackground);
        var paneWidth = Math.Clamp(ReadFloat(navigationView, "openPaneLength", 248), 220, 320);
        var titleBarHeight = 48;

        DrawRect(canvas, paint, new SKRect(0, 0, width, height), AppBackground);
        DrawRect(canvas, paint, new SKRect(0, 0, width, titleBarHeight), Surface);
        DrawLine(canvas, paint, 0, titleBarHeight, width, titleBarHeight, Stroke);
        DrawText(canvas, paint, titleFont, ReadString(root, "title") ?? "WinUI Shell", 24, 31, TextPrimary);

        DrawRect(canvas, paint, new SKRect(0, titleBarHeight, paneWidth, height), PaneBackground);
        DrawLine(canvas, paint, paneWidth, titleBarHeight, paneWidth, height, Stroke);
        DrawText(canvas, paint, bodyFont, "Navigation", 20, titleBarHeight + 34, TextSecondary);

        var selectedItem = ReadString(navigationView, "selectedItem");
        var menuItems = navigationView.Children.Where(IsNavigationViewItem).ToArray();
        var y = titleBarHeight + 58;
        foreach (var item in menuItems)
        {
            var selected = string.Equals(item.Name, selectedItem, StringComparison.Ordinal);
            var row = new SKRect(12, y, paneWidth - 12, y + 40);
            if (selected)
            {
                DrawRoundRect(canvas, paint, row, 8, AccentSoft);
                DrawRoundRect(canvas, paint, new SKRect(row.Left, row.Top + 7, row.Left + 4, row.Bottom - 7), 2, Accent);
            }

            DrawText(canvas, paint, bodyFont, ReadNavigationLabel(item), 42, y + 26, selected ? Accent : TextPrimary);
            DrawIconDot(canvas, paint, 26, y + 20, selected ? Accent : TextSecondary);
            y += 44;
        }

        RenderPaneFooter(canvas, navigationView, paint, bodyFont, smallFont, paneWidth, height);
        RenderContentArea(canvas, navigationView, paint, titleFont, bodyFont, smallFont, paneWidth, titleBarHeight, width, height);
    }

    private static void RenderPaneFooter(
        SKCanvas canvas,
        UiNode navigationView,
        SKPaint paint,
        SKFont bodyFont,
        SKFont smallFont,
        float paneWidth,
        int height)
    {
        var footer = navigationView.Children.FirstOrDefault(child => IsStackPanel(child) && child.Children.Any());
        if (footer is null)
        {
            return;
        }

        var account = FindFirst(footer, node => node.Name is "AccountFooter") ?? footer.Children.FirstOrDefault();
        var displayName = FindByName(footer, "AccountDisplayNameTextBlock");
        var username = FindByName(footer, "AccountUsernameTextBlock");
        var logout = FindByName(footer, "LogoutButton");

        var footerTop = height - 154;
        DrawLine(canvas, paint, 16, footerTop - 16, paneWidth - 16, footerTop - 16, Stroke);
        if (account is not null)
        {
            DrawRoundRect(canvas, paint, new SKRect(14, footerTop, paneWidth - 14, footerTop + 74), 10, Surface);
            DrawRoundRect(canvas, paint, new SKRect(26, footerTop + 17, 62, footerTop + 53), 18, AccentSoft);
            DrawText(canvas, paint, bodyFont, ReadText(displayName) ?? ReadString(account, "automationName") ?? "Account", 76, footerTop + 31, TextPrimary);
            DrawText(canvas, paint, smallFont, ReadText(username) ?? string.Empty, 76, footerTop + 52, TextSecondary);
        }

        if (logout is not null)
        {
            var row = new SKRect(14, footerTop + 90, paneWidth - 14, footerTop + 130);
            DrawRoundRect(canvas, paint, row, 8, Surface);
            DrawText(canvas, paint, bodyFont, ReadControlLabel(logout, "Sign out"), row.Left + 14, row.Top + 26, TextPrimary);
        }
    }

    private static void RenderContentArea(
        SKCanvas canvas,
        UiNode navigationView,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        SKFont smallFont,
        float paneWidth,
        int titleBarHeight,
        int width,
        int height)
    {
        var frame = navigationView.Children.LastOrDefault(IsFrame);
        var contentX = paneWidth + 32;
        var contentY = titleBarHeight + 36;
        var contentWidth = width - contentX - 32;
        var contentHeight = height - contentY - 32;

        DrawText(canvas, paint, titleFont, ReadContentTitle(frame, navigationView), contentX, contentY, TextPrimary);
        DrawText(canvas, paint, bodyFont, "macOS compatibility renderer preview", contentX, contentY + 28, TextSecondary);

        var card = new SKRect(contentX, contentY + 54, contentX + contentWidth, contentY + contentHeight);
        DrawRoundRect(canvas, paint, card, 14, Surface);
        DrawRoundRectStroke(canvas, paint, card, 14, Stroke);

        var textBlocks = frame is null
            ? Array.Empty<UiNode>()
            : Flatten(frame).Where(IsTextBlock).ToArray();
        var y = card.Top + 42;
        foreach (var textBlock in textBlocks.Take(8))
        {
            DrawText(canvas, paint, bodyFont, ReadText(textBlock) ?? textBlock.Name ?? "Text", card.Left + 30, y, TextPrimary);
            y += 28;
        }

        if (textBlocks.Length == 0)
        {
            DrawText(canvas, paint, bodyFont, "No visible content text was exported for this frame.", card.Left + 30, y, TextSecondary);
            DrawText(canvas, paint, smallFont, "The renderer still captured the shell, navigation, and structural state.", card.Left + 30, y + 26, TextSecondary);
        }
    }

    private static void RenderDiagnosticSnapshot(
        SKCanvas canvas,
        UiNode root,
        SKPaint paint,
        SKFont titleFont,
        SKFont bodyFont,
        int width,
        int height)
    {
        var nodes = Flatten(root).Select((node, index) => (Node: node, Index: index, Depth: DepthOf(root, node))).ToArray();
        canvas.Clear(new SKColor(0x10, 0x18, 0x20));
        DrawText(canvas, paint, titleFont, "WinUI3 Mac Test Snapshot", 24, 28, new SKColor(0xf6, 0xf7, 0xf9));

        foreach (var item in nodes)
        {
            var y = 56 + item.Index * 30;
            var x = 24 + item.Depth * 28;
            var label = item.Node.Name is null ? item.Node.Type : $"{item.Node.Type} #{item.Node.Name}";
            DrawRoundRect(canvas, paint, new SKRect(x, y - 18, Math.Max(x + 120, width - 24), y + 6), 4, FillForDepth(item.Depth));
            DrawText(canvas, paint, bodyFont, label, x + 8, y, new SKColor(0xf6, 0xf7, 0xf9));
        }
    }

    private static (int Width, int Height) MeasureDiagnosticSnapshot(UiNode root)
    {
        var count = Flatten(root).Count();
        return (960, Math.Max(180, 40 + count * 30));
    }

    private static string ReadContentTitle(UiNode? frame, UiNode navigationView)
    {
        var firstText = frame is null ? null : Flatten(frame).FirstOrDefault(IsTextBlock);
        if (!string.IsNullOrWhiteSpace(ReadText(firstText)))
        {
            return ReadText(firstText)!;
        }

        var selectedItem = ReadString(navigationView, "selectedItem");
        var selected = navigationView.Children.FirstOrDefault(child => child.Name == selectedItem);
        return selected is null ? "Content" : ReadNavigationLabel(selected);
    }

    private static string ReadNavigationLabel(UiNode item)
    {
        return ReadControlLabel(item, ToTitle(ReadString(item, "tag") ?? item.Name?.Replace("NavigationItem", string.Empty, StringComparison.Ordinal) ?? "Item"));
    }

    private static string ReadControlLabel(UiNode item, string fallback)
    {
        return ReadString(item, "automationName") ??
            ReadString(item, "content") ??
            ReadText(item) ??
            fallback;
    }

    private static string? ReadText(UiNode? node)
    {
        return node is null ? null : ReadString(node, "text");
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

    private static int DepthOf(UiNode root, UiNode target, int depth = 0)
    {
        if (ReferenceEquals(root, target))
        {
            return depth;
        }

        foreach (var child in root.Children)
        {
            var result = DepthOf(child, target, depth + 1);
            if (result >= 0)
            {
                return result;
            }
        }

        return -1;
    }

    private static UiNode? FindFirst(UiNode root, Func<UiNode, bool> predicate)
    {
        return Flatten(root).FirstOrDefault(predicate);
    }

    private static UiNode? FindByName(UiNode root, string name)
    {
        return FindFirst(root, node => string.Equals(node.Name, name, StringComparison.Ordinal));
    }

    private static bool IsNavigationView(UiNode node)
    {
        return node.Type.EndsWith(".NavigationView", StringComparison.Ordinal);
    }

    private static bool IsNavigationViewItem(UiNode node)
    {
        return node.Type.EndsWith(".NavigationViewItem", StringComparison.Ordinal);
    }

    private static bool IsStackPanel(UiNode node)
    {
        return node.Type.EndsWith(".StackPanel", StringComparison.Ordinal);
    }

    private static bool IsFrame(UiNode node)
    {
        return node.Type.EndsWith(".Frame", StringComparison.Ordinal);
    }

    private static bool IsTextBlock(UiNode node)
    {
        return node.Type.EndsWith(".TextBlock", StringComparison.Ordinal);
    }

    private static string? ReadString(UiNode node, string key)
    {
        return node.Properties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static float ReadFloat(UiNode node, string key, float fallback)
    {
        if (!node.Properties.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return float.TryParse(value.ToString(), out var number) ? number : fallback;
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

    private static void DrawText(SKCanvas canvas, SKPaint paint, SKFont font, string text, float x, float y, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private static void DrawIconDot(SKCanvas canvas, SKPaint paint, float x, float y, SKColor color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color;
        canvas.DrawCircle(x, y, 5, paint);
    }

    private static SKColor FillForDepth(int depth)
    {
        return (depth % 3) switch
        {
            0 => new SKColor(0x24, 0x43, 0x5c),
            1 => new SKColor(0x2f, 0x5f, 0x53),
            _ => new SKColor(0x5b, 0x4b, 0x73)
        };
    }
}
