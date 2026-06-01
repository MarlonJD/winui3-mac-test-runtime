using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

public sealed record PixelDiffBounds(
    int X,
    int Y,
    int Width,
    int Height);

public sealed record PixelDiffResult(
    string SchemaVersion,
    int Width,
    int Height,
    int ChangedPixelCount,
    double ChangedPixelPercentage,
    int MaxChannelDelta,
    double MeanAbsoluteError,
    double RootMeanSquaredError,
    PixelDiffBounds? BoundingBox,
    VisualThresholds Thresholds,
    string Status,
    string? Message);

public static class PixelDiff
{
    public static PixelDiffResult Compare(
        string referencePath,
        string runtimePath,
        string diffOutputPath,
        VisualThresholds thresholds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referencePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(diffOutputPath);
        ArgumentNullException.ThrowIfNull(thresholds);

        using var reference = SKBitmap.Decode(referencePath)
            ?? throw new InvalidOperationException($"Could not decode reference image '{referencePath}'.");
        using var runtime = SKBitmap.Decode(runtimePath)
            ?? throw new InvalidOperationException($"Could not decode runtime image '{runtimePath}'.");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(diffOutputPath))!);

        if (reference.Width != runtime.Width || reference.Height != runtime.Height)
        {
            WriteDimensionMismatchDiff(reference, runtime, diffOutputPath);
            return new PixelDiffResult(
                SchemaVersion: "0.1",
                Width: Math.Max(reference.Width, runtime.Width),
                Height: Math.Max(reference.Height, runtime.Height),
                ChangedPixelCount: Math.Max(reference.Width * reference.Height, runtime.Width * runtime.Height),
                ChangedPixelPercentage: 100,
                MaxChannelDelta: 255,
                MeanAbsoluteError: 255,
                RootMeanSquaredError: 255,
                BoundingBox: new PixelDiffBounds(0, 0, Math.Max(reference.Width, runtime.Width), Math.Max(reference.Height, runtime.Height)),
                Thresholds: thresholds,
                Status: "failed",
                Message: $"Image dimensions differ. Reference is {reference.Width}x{reference.Height}; runtime is {runtime.Width}x{runtime.Height}.");
        }

        var width = reference.Width;
        var height = reference.Height;
        using var diff = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var changed = 0;
        var maxDelta = 0;
        long absoluteError = 0;
        long squaredError = 0;
        var minX = width;
        var minY = height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var expected = reference.GetPixel(x, y);
                var actual = runtime.GetPixel(x, y);
                var deltaR = Math.Abs(expected.Red - actual.Red);
                var deltaG = Math.Abs(expected.Green - actual.Green);
                var deltaB = Math.Abs(expected.Blue - actual.Blue);
                var deltaA = Math.Abs(expected.Alpha - actual.Alpha);
                var pixelMax = Math.Max(Math.Max(deltaR, deltaG), Math.Max(deltaB, deltaA));
                maxDelta = Math.Max(maxDelta, pixelMax);
                absoluteError += deltaR + deltaG + deltaB + deltaA;
                squaredError += deltaR * deltaR + deltaG * deltaG + deltaB * deltaB + deltaA * deltaA;

                if (pixelMax > 0)
                {
                    changed++;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                    diff.SetPixel(x, y, new SKColor(0xff, 0x2d, 0x55));
                }
                else
                {
                    var gray = (byte)((expected.Red + expected.Green + expected.Blue) / 3);
                    diff.SetPixel(x, y, new SKColor(gray, gray, gray, 0x66));
                }
            }
        }

        WritePng(diff, diffOutputPath);

        var channelCount = Math.Max(1, width * height * 4);
        var changedPercentage = changed * 100.0 / Math.Max(1, width * height);
        var meanAbsoluteError = absoluteError / (double)channelCount;
        var rootMeanSquaredError = Math.Sqrt(squaredError / (double)channelCount);
        var passed =
            changedPercentage <= thresholds.ChangedPixelPercentage &&
            maxDelta <= thresholds.MaxChannelDelta &&
            meanAbsoluteError <= thresholds.MeanAbsoluteError &&
            rootMeanSquaredError <= thresholds.RootMeanSquaredError;

        return new PixelDiffResult(
            SchemaVersion: "0.1",
            Width: width,
            Height: height,
            ChangedPixelCount: changed,
            ChangedPixelPercentage: Math.Round(changedPercentage, 6),
            MaxChannelDelta: maxDelta,
            MeanAbsoluteError: Math.Round(meanAbsoluteError, 6),
            RootMeanSquaredError: Math.Round(rootMeanSquaredError, 6),
            BoundingBox: changed == 0 ? null : new PixelDiffBounds(minX, minY, maxX - minX + 1, maxY - minY + 1),
            Thresholds: thresholds,
            Status: passed ? "passed" : "failed",
            Message: null);
    }

    private static void WriteDimensionMismatchDiff(SKBitmap reference, SKBitmap runtime, string outputPath)
    {
        var width = Math.Max(reference.Width, runtime.Width);
        var height = Math.Max(reference.Height, runtime.Height);
        using var diff = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(diff);
        canvas.Clear(new SKColor(0xff, 0x2d, 0x55));
        WritePng(diff, outputPath);
    }

    private static void WritePng(SKBitmap bitmap, string outputPath)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.Create(outputPath);
        data.SaveTo(stream);
    }
}
