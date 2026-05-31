using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

public sealed class SkiaSnapshotRenderer : ISnapshotRenderer
{
    public async Task<SnapshotResult> RenderAsync(
        UiTreeDocument tree,
        string screenshotsDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tree);

        Directory.CreateDirectory(screenshotsDirectory);
        var width = 960;
        var rowHeight = 30;
        var nodes = Flatten(tree.Root).ToArray();
        var height = Math.Max(180, 40 + nodes.Length * rowHeight);
        var path = Path.Combine(screenshotsDirectory, "snapshot.png");

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0x10, 0x18, 0x20));

        using var titlePaint = new SKPaint
        {
            Color = new SKColor(0xf6, 0xf7, 0xf9),
            IsAntialias = true
        };
        using var typeface = SKTypeface.FromFamilyName("Menlo");
        using var titleFont = new SKFont(typeface, 16);
        using var textFont = new SKFont(typeface, 13);
        canvas.DrawText("WinUI3 Mac Test Snapshot", 24, 28, SKTextAlign.Left, titleFont, titlePaint);

        using var rowPaint = new SKPaint { IsAntialias = true };
        using var textPaint = new SKPaint
        {
            Color = new SKColor(0xf6, 0xf7, 0xf9),
            IsAntialias = true
        };

        for (var index = 0; index < nodes.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (node, depth) = nodes[index];
            var y = 56 + index * rowHeight;
            var x = 24 + depth * 28;
            var label = node.Name is null ? node.Type : $"{node.Type} #{node.Name}";
            rowPaint.Color = FillForDepth(depth);
            canvas.DrawRoundRect(
                new SKRect(x, y - 18, Math.Max(x + 120, width - 24), y + 6),
                rx: 4,
                ry: 4,
                rowPaint);
            canvas.DrawText(label, x + 8, y, SKTextAlign.Left, textFont, textPaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        await using var stream = File.Create(path);
        data.SaveTo(stream);

        return new SnapshotResult("0.1", "skia-png", path, width, height, IsNonBlank: nodes.Length > 0);
    }

    private static IEnumerable<(UiNode Node, int Depth)> Flatten(UiNode root, int depth = 0)
    {
        yield return (root, depth);
        foreach (var child in root.Children)
        {
            foreach (var nested in Flatten(child, depth + 1))
            {
                yield return nested;
            }
        }
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
