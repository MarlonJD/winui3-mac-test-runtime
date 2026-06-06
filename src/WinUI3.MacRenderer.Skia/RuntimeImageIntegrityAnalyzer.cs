using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

internal static class RuntimeImageIntegrityAnalyzer
{
    public static RuntimeImageIntegrity Analyze(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return new RuntimeImageIntegrity(
                "failed",
                IsNonBlank: false,
                bitmap.Width,
                bitmap.Height,
                DistinctColorCount: 0,
                NonBackgroundPixelPercentage: 0,
                "Runtime image has no drawable pixel area.");
        }

        var background = bitmap.GetPixel(0, 0);
        var distinct = new HashSet<uint>();
        var nonBackgroundPixels = 0;
        var totalPixels = bitmap.Width * bitmap.Height;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                distinct.Add(Pack(color));
                if (color != background)
                {
                    nonBackgroundPixels++;
                }
            }
        }

        var nonBackgroundPercentage = totalPixels == 0
            ? 0
            : Math.Round(nonBackgroundPixels * 100d / totalPixels, 4);
        var isNonBlank = distinct.Count > 1 && nonBackgroundPixels > 0;

        return new RuntimeImageIntegrity(
            isNonBlank ? "passed" : "failed",
            isNonBlank,
            bitmap.Width,
            bitmap.Height,
            distinct.Count,
            nonBackgroundPercentage,
            isNonBlank
                ? "Runtime image contains visible pixels that differ from the background."
                : "Runtime image is blank or effectively single-color.");
    }

    private static uint Pack(SKColor color)
    {
        return ((uint)color.Alpha << 24) |
            ((uint)color.Red << 16) |
            ((uint)color.Green << 8) |
            color.Blue;
    }
}
