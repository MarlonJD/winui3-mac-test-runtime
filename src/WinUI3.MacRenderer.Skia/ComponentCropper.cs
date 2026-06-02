using System.Globalization;
using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

public static class ComponentCropper
{
    public static ComponentEvidenceDocument WriteCrops(
        ComponentEvidenceDocument evidence,
        string runtimeImagePath,
        string? referenceImagePath,
        string outputDirectory,
        double scale,
        VisualThresholds scenarioThresholds,
        NativeReferenceProvenance? nativeReferenceProvenance = null)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeImagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(scenarioThresholds);

        using var runtime = Decode(runtimeImagePath);
        using var reference = string.IsNullOrWhiteSpace(referenceImagePath) ? null : Decode(referenceImagePath);
        var cropsDirectory = Path.Combine(outputDirectory, "components");
        Directory.CreateDirectory(cropsDirectory);

        var cropEvidence = new Dictionary<string, ComponentCropEvidence>(StringComparer.Ordinal);
        foreach (var component in evidence.Components)
        {
            var crop = WriteCrop(component, runtime, reference, cropsDirectory, scale, scenarioThresholds, nativeReferenceProvenance);
            cropEvidence[ComponentEvidenceBuilder.ComponentKey(component.Component, component.Target)] = crop;
        }

        return ComponentEvidenceBuilder.WithComponentCrops(evidence, cropEvidence);
    }

    public static ComponentCropBounds? BoundsFor(UiLayoutBox? layout, int imageWidth, int imageHeight, double scale)
    {
        if (layout is null || layout.Width <= 0 || layout.Height <= 0)
        {
            return null;
        }

        var x = (int)Math.Floor(layout.X * scale);
        var y = (int)Math.Floor(layout.Y * scale);
        var right = (int)Math.Ceiling((layout.X + layout.Width) * scale);
        var bottom = (int)Math.Ceiling((layout.Y + layout.Height) * scale);
        x = Math.Clamp(x, 0, Math.Max(0, imageWidth - 1));
        y = Math.Clamp(y, 0, Math.Max(0, imageHeight - 1));
        right = Math.Clamp(right, x + 1, imageWidth);
        bottom = Math.Clamp(bottom, y + 1, imageHeight);

        return new ComponentCropBounds(x, y, right - x, bottom - y);
    }

    public static bool IsBlankCrop(string imagePath)
    {
        using var bitmap = Decode(imagePath);
        return IsBlank(bitmap);
    }

    private static ComponentCropEvidence WriteCrop(
        ComponentEvidenceEntry component,
        SKBitmap runtime,
        SKBitmap? reference,
        string cropsDirectory,
        double scale,
        VisualThresholds scenarioThresholds,
        NativeReferenceProvenance? nativeReferenceProvenance)
    {
        var thresholds = component.ComponentThresholds ?? scenarioThresholds;
        var bounds = BoundsFor(component.LayoutRegion, runtime.Width, runtime.Height, scale);
        var strictClaim = component.CatalogStatus is "supported" or "partial";
        if (bounds is null)
        {
            return new ComponentCropEvidence(
                Status: strictClaim ? "failed" : "not-applicable",
                Bounds: null,
                NativeReferencePath: null,
                MacRuntimePath: null,
                PixelDiffPath: null,
                RuntimeBlank: null,
                Thresholds: thresholds,
                ChangedPixelPercentage: null,
                MeanAbsoluteError: null,
                RootMeanSquaredError: null,
                Message: strictClaim
                    ? "Claimed component is missing a positive target layout region."
                    : "Diagnostic or excluded component does not require crop evidence.")
            {
                NativeReferenceProvenance = nativeReferenceProvenance
            };
        }

        var componentDirectory = Path.Combine(cropsDirectory, SafeName(component.Component, component.Target));
        Directory.CreateDirectory(componentDirectory);

        var runtimeCropPath = Path.Combine(componentDirectory, "mac-runtime.png");
        WriteCrop(runtime, bounds, runtimeCropPath);
        var blank = IsBlankCrop(runtimeCropPath);
        if (strictClaim && string.Equals(component.VisualGrade, "not-rendered", StringComparison.Ordinal))
        {
            return CropFailure(component, bounds, runtimeCropPath, null, null, blank, thresholds, nativeReferenceProvenance, "Claimed component is not-rendered.");
        }

        if (strictClaim && blank)
        {
            return CropFailure(component, bounds, runtimeCropPath, null, null, blank, thresholds, nativeReferenceProvenance, "Claimed component crop is blank.");
        }

        if (reference is null)
        {
            return new ComponentCropEvidence(
                Status: strictClaim ? "reference-skipped" : "not-applicable",
                Bounds: bounds,
                NativeReferencePath: null,
                MacRuntimePath: runtimeCropPath,
                PixelDiffPath: null,
                RuntimeBlank: blank,
                Thresholds: thresholds,
                ChangedPixelPercentage: null,
                MeanAbsoluteError: null,
                RootMeanSquaredError: null,
                Message: "No native reference image was provided for component crop comparison.")
            {
                NativeReferenceProvenance = nativeReferenceProvenance
            };
        }

        var referenceBounds = BoundsFor(component.LayoutRegion, reference.Width, reference.Height, scale);
        if (referenceBounds is null)
        {
            return CropFailure(component, bounds, runtimeCropPath, null, null, blank, thresholds, nativeReferenceProvenance, "Native reference crop bounds are outside the reference image.");
        }

        var referenceCropPath = Path.Combine(componentDirectory, "windows-reference.png");
        var diffPath = Path.Combine(componentDirectory, "pixel-diff.png");
        WriteCrop(reference, referenceBounds, referenceCropPath);
        var diff = PixelDiff.Compare(referenceCropPath, runtimeCropPath, diffPath, thresholds);
        var status = strictClaim && diff.Status == "failed" ? "failed" : diff.Status;

        return new ComponentCropEvidence(
            Status: status,
            Bounds: bounds,
            NativeReferencePath: referenceCropPath,
            MacRuntimePath: runtimeCropPath,
            PixelDiffPath: diffPath,
            RuntimeBlank: blank,
            Thresholds: thresholds,
            ChangedPixelPercentage: diff.ChangedPixelPercentage,
            MeanAbsoluteError: diff.MeanAbsoluteError,
            RootMeanSquaredError: diff.RootMeanSquaredError,
            Message: diff.Message)
        {
            NativeReferenceProvenance = nativeReferenceProvenance
        };
    }

    private static ComponentCropEvidence CropFailure(
        ComponentEvidenceEntry component,
        ComponentCropBounds bounds,
        string runtimeCropPath,
        string? referenceCropPath,
        string? diffPath,
        bool blank,
        VisualThresholds thresholds,
        NativeReferenceProvenance? nativeReferenceProvenance,
        string message)
    {
        return new ComponentCropEvidence(
            Status: component.CatalogStatus is "supported" or "partial" ? "failed" : "not-applicable",
            Bounds: bounds,
            NativeReferencePath: referenceCropPath,
            MacRuntimePath: runtimeCropPath,
            PixelDiffPath: diffPath,
            RuntimeBlank: blank,
            Thresholds: thresholds,
            ChangedPixelPercentage: null,
            MeanAbsoluteError: null,
            RootMeanSquaredError: null,
            Message: message)
        {
            NativeReferenceProvenance = nativeReferenceProvenance
        };
    }

    private static SKBitmap Decode(string imagePath)
    {
        return SKBitmap.Decode(imagePath)
            ?? throw new InvalidOperationException($"Could not decode image '{imagePath}'.");
    }

    private static void WriteCrop(SKBitmap source, ComponentCropBounds bounds, string outputPath)
    {
        using var crop = new SKBitmap(bounds.Width, bounds.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(crop);
        var sourceRect = new SKRectI(bounds.X, bounds.Y, bounds.X + bounds.Width, bounds.Y + bounds.Height);
        var destinationRect = new SKRect(0, 0, bounds.Width, bounds.Height);
        canvas.DrawBitmap(source, sourceRect, destinationRect);
        using var image = SKImage.FromBitmap(crop);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.Create(outputPath);
        data.SaveTo(stream);
    }

    private static bool IsBlank(SKBitmap bitmap)
    {
        if (bitmap.Width == 0 || bitmap.Height == 0)
        {
            return true;
        }

        var first = bitmap.GetPixel(0, 0);
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) != first)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string SafeName(string component, string? target)
    {
        var raw = string.IsNullOrWhiteSpace(target) ? component : component + "-" + target;
        var chars = raw
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray();
        return string.Join(
            "-",
            new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries))
            .ToLower(CultureInfo.InvariantCulture);
    }
}
