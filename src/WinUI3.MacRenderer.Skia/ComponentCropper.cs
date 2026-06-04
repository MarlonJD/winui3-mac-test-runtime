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
        NativeReferenceProvenance? nativeReferenceProvenance = null,
        bool useRelativePaths = false,
        NativeReferenceTargetDocument? nativeReferenceTargets = null)
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
            var crop = WriteCrop(component, runtime, reference, outputDirectory, cropsDirectory, scale, scenarioThresholds, nativeReferenceProvenance, nativeReferenceTargets, useRelativePaths);
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

    public static ComponentCropBounds? BoundsFor(NativeReferenceBounds? bounds, int imageWidth, int imageHeight, double scale)
    {
        if (bounds is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return null;
        }

        var x = (int)Math.Floor(bounds.X * scale);
        var y = (int)Math.Floor(bounds.Y * scale);
        var right = (int)Math.Ceiling((bounds.X + bounds.Width) * scale);
        var bottom = (int)Math.Ceiling((bounds.Y + bounds.Height) * scale);
        if (x < 0 || y < 0 || right > imageWidth || bottom > imageHeight)
        {
            return null;
        }

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
        string outputDirectory,
        string cropsDirectory,
        double scale,
        VisualThresholds scenarioThresholds,
        NativeReferenceProvenance? nativeReferenceProvenance,
        NativeReferenceTargetDocument? nativeReferenceTargets,
        bool useRelativePaths)
    {
        var thresholds = component.ComponentThresholds ?? scenarioThresholds;
        var bounds = BoundsFor(component.LayoutRegion, runtime.Width, runtime.Height, scale);
        var strictClaim = component.CatalogStatus is "supported" or "partial";
        var nativeTarget = FindNativeReferenceTarget(nativeReferenceTargets, component);
        if (bounds is null)
        {
            return new ComponentCropEvidence(
                Status: strictClaim ? "failed" : "not-applicable",
                Bounds: null,
                NativeReferenceBounds: null,
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
                NativeReferenceProvenance = nativeReferenceProvenance,
                NativeReferenceTarget = nativeTarget,
                NativeReferenceReadinessStatus = "missing-runtime-bounds",
                NativeReferenceReadinessReason = "The macOS runtime did not expose a positive layout region for this component.",
                NativeReferenceRequiredAction = "Fix macOS runtime target layout before comparing native reference crops.",
                NativeReferenceBoundsSource = "missing",
                NativeReferenceBoundsValidForPromotion = false,
                NativeReferenceIntegrityBlockerReason = "The macOS runtime did not expose a positive layout region for this component.",
                NativeReferenceReadiness = Readiness(
                    "missing-runtime-bounds",
                    "The macOS runtime did not expose a positive layout region for this component.",
                    "Fix macOS runtime target layout before comparing native reference crops.",
                    readyForPromotion: false),
                NativeReferenceCropSize = null,
                MacRuntimeCropSize = null,
                NativeReferenceBoundsDelta = null
            };
        }

        var componentDirectory = Path.Combine(cropsDirectory, SafeName(component.Component, component.Target));
        Directory.CreateDirectory(componentDirectory);

        var runtimeCropPath = Path.Combine(componentDirectory, "mac-runtime.png");
        WriteCrop(runtime, bounds, runtimeCropPath);
        var blank = IsBlankCrop(runtimeCropPath);
        if (strictClaim && string.Equals(component.VisualGrade, "not-rendered", StringComparison.Ordinal))
        {
            return CropFailure(component, bounds, null, runtimeCropPath, null, null, blank, thresholds, nativeReferenceProvenance, nativeTarget, "Claimed component is not-rendered.", outputDirectory, useRelativePaths);
        }

        if (strictClaim && blank)
        {
            return CropFailure(component, bounds, null, runtimeCropPath, null, null, blank, thresholds, nativeReferenceProvenance, nativeTarget, "Claimed component crop is blank.", outputDirectory, useRelativePaths);
        }

        if (reference is null)
        {
            return new ComponentCropEvidence(
                Status: strictClaim ? "reference-skipped" : "not-applicable",
                Bounds: bounds,
                NativeReferenceBounds: null,
                NativeReferencePath: null,
                MacRuntimePath: ArtifactPath(outputDirectory, runtimeCropPath, useRelativePaths),
                PixelDiffPath: null,
                RuntimeBlank: blank,
                Thresholds: thresholds,
                ChangedPixelPercentage: null,
                MeanAbsoluteError: null,
                RootMeanSquaredError: null,
                Message: "No native reference image was provided for component crop comparison.")
            {
                NativeReferenceProvenance = nativeReferenceProvenance,
                NativeReferenceTarget = nativeTarget,
                NativeReferenceReadinessStatus = "missing-native-reference-crop",
                NativeReferenceReadinessReason = "No native reference image was provided for component crop comparison.",
                NativeReferenceRequiredAction = "Capture and import a native Windows reference image with target bounds.",
                NativeReferenceBoundsSource = "missing",
                NativeReferenceBoundsValidForPromotion = false,
                NativeReferenceIntegrityBlockerReason = "No native reference image was provided for component crop comparison.",
                NativeReferenceReadiness = Readiness(
                    "missing-native-reference-crop",
                    "No native reference image was provided for component crop comparison.",
                    "Capture and import a native Windows reference image with target bounds.",
                    readyForPromotion: false),
                NativeReferenceCropSize = null,
                MacRuntimeCropSize = CropSize(bounds),
                NativeReferenceBoundsDelta = null
            };
        }

        var referenceBounds = BoundsFor(nativeTarget?.Bounds, reference.Width, reference.Height, scale);
        if (referenceBounds is null)
        {
            return CropFailure(
                component,
                bounds,
                null,
                runtimeCropPath,
                null,
                null,
                blank,
                thresholds,
                nativeReferenceProvenance,
                nativeTarget,
                nativeTarget is null
                    ? "Native reference target bounds are missing; refusing to crop the Windows image from macOS/runtime layout bounds."
                    : "Native reference target bounds are outside the reference image.",
                outputDirectory,
                useRelativePaths);
        }

        var referenceCropPath = Path.Combine(componentDirectory, "windows-reference.png");
        var diffPath = Path.Combine(componentDirectory, "pixel-diff.png");
        WriteCrop(reference, referenceBounds, referenceCropPath);
        if (referenceBounds.Width != bounds.Width || referenceBounds.Height != bounds.Height)
        {
            var mismatchDiff = PixelDiff.Compare(referenceCropPath, runtimeCropPath, diffPath, thresholds);
            return CropFailure(
                component,
                bounds,
                referenceBounds,
                runtimeCropPath,
                referenceCropPath,
                diffPath: diffPath,
                blank: blank,
                thresholds: thresholds,
                nativeReferenceProvenance: nativeReferenceProvenance,
                nativeReferenceTarget: nativeTarget,
                message: $"Native and macOS crop dimensions differ. Native crop is {referenceBounds.Width}x{referenceBounds.Height}; macOS crop is {bounds.Width}x{bounds.Height}. Phase -1 does not normalize crop sizes.",
                outputDirectory: outputDirectory,
                useRelativePaths: useRelativePaths,
                readinessStatus: "native-crop-size-mismatch",
                diff: mismatchDiff);
        }

        var diff = PixelDiff.Compare(referenceCropPath, runtimeCropPath, diffPath, thresholds);
        var status = strictClaim && diff.Status == "failed" ? "failed" : diff.Status;

        return new ComponentCropEvidence(
            Status: status,
            Bounds: bounds,
            NativeReferenceBounds: referenceBounds,
            NativeReferencePath: ArtifactPath(outputDirectory, referenceCropPath, useRelativePaths),
            MacRuntimePath: ArtifactPath(outputDirectory, runtimeCropPath, useRelativePaths),
            PixelDiffPath: ArtifactPath(outputDirectory, diffPath, useRelativePaths),
            RuntimeBlank: blank,
            Thresholds: thresholds,
            ChangedPixelPercentage: diff.ChangedPixelPercentage,
            MeanAbsoluteError: diff.MeanAbsoluteError,
            RootMeanSquaredError: diff.RootMeanSquaredError,
            Message: diff.Message)
        {
            NativeReferenceProvenance = nativeReferenceProvenance,
            NativeReferenceTarget = nativeTarget,
            NativeReferenceReadinessStatus = "ready",
            NativeReferenceReadinessReason = "Native crop uses Windows native element bounds from native-reference-targets.json.",
            NativeReferenceRequiredAction = "Keep the native target export with the Windows reference artifact.",
            NativeReferenceBoundsSource = "windows-native-element-bounds",
            NativeReferenceBoundsValidForPromotion = true,
            NativeReferenceIntegrityBlockerReason = "none",
            NativeReferenceReadiness = Readiness(
                "ready",
                "Native crop uses Windows native element bounds from native-reference-targets.json.",
                "Keep the native target export with the Windows reference artifact.",
                readyForPromotion: true),
            NativeReferenceCropSize = CropSize(referenceBounds),
            MacRuntimeCropSize = CropSize(bounds),
            NativeReferenceBoundsDelta = BoundsDelta(referenceBounds, bounds)
        };
    }

    private static ComponentCropEvidence CropFailure(
        ComponentEvidenceEntry component,
        ComponentCropBounds bounds,
        ComponentCropBounds? nativeReferenceBounds,
        string runtimeCropPath,
        string? referenceCropPath,
        string? diffPath,
        bool blank,
        VisualThresholds thresholds,
        NativeReferenceProvenance? nativeReferenceProvenance,
        NativeReferenceTarget? nativeReferenceTarget,
        string message,
        string outputDirectory,
        bool useRelativePaths,
        string? readinessStatus = null,
        PixelDiffResult? diff = null)
    {
        var status = readinessStatus ?? (nativeReferenceTarget is null ? "needs-native-crop-bounds" : "invalid-native-crop-bounds");
        return new ComponentCropEvidence(
            Status: component.CatalogStatus is "supported" or "partial" ? "failed" : "not-applicable",
            Bounds: bounds,
            NativeReferenceBounds: nativeReferenceBounds,
            NativeReferencePath: ArtifactPath(outputDirectory, referenceCropPath, useRelativePaths),
            MacRuntimePath: ArtifactPath(outputDirectory, runtimeCropPath, useRelativePaths),
            PixelDiffPath: ArtifactPath(outputDirectory, diffPath, useRelativePaths),
            RuntimeBlank: blank,
            Thresholds: thresholds,
            ChangedPixelPercentage: diff?.ChangedPixelPercentage,
            MeanAbsoluteError: diff?.MeanAbsoluteError,
            RootMeanSquaredError: diff?.RootMeanSquaredError,
            Message: message)
        {
            NativeReferenceProvenance = nativeReferenceProvenance,
            NativeReferenceTarget = nativeReferenceTarget,
            NativeReferenceReadinessStatus = status,
            NativeReferenceReadinessReason = message,
            NativeReferenceRequiredAction = nativeReferenceTarget is null
                ? "Re-run the Windows native reference workflow and keep native-reference-targets.json with the screenshot artifact."
                : "Fix the exported Windows native target bounds and regenerate component evidence.",
            NativeReferenceBoundsSource = nativeReferenceTarget is null ? "missing" : "windows-native-element-bounds",
            NativeReferenceBoundsValidForPromotion = false,
            NativeReferenceIntegrityBlockerReason = message,
            NativeReferenceReadiness = Readiness(
                status,
                message,
                nativeReferenceTarget is null
                    ? "Re-run the Windows native reference workflow and keep native-reference-targets.json with the screenshot artifact."
                    : "Fix the exported Windows native target bounds and regenerate component evidence.",
                readyForPromotion: false),
            NativeReferenceCropSize = nativeReferenceBounds is null ? null : CropSize(nativeReferenceBounds),
            MacRuntimeCropSize = CropSize(bounds),
            NativeReferenceBoundsDelta = nativeReferenceBounds is null ? null : BoundsDelta(nativeReferenceBounds, bounds)
        };
    }

    private static NativeReferenceTarget? FindNativeReferenceTarget(
        NativeReferenceTargetDocument? targets,
        ComponentEvidenceEntry component)
    {
        if (targets is null || string.IsNullOrWhiteSpace(component.Target))
        {
            return null;
        }

        return targets.Targets
            .Where(target => string.Equals(target.Target, component.Target, StringComparison.Ordinal))
            .OrderByDescending(target => string.Equals(target.Component, component.Component, StringComparison.Ordinal))
            .FirstOrDefault();
    }

    private static SKBitmap Decode(string imagePath)
    {
        return SKBitmap.Decode(imagePath)
            ?? throw new InvalidOperationException($"Could not decode image '{imagePath}'.");
    }

    private static string? ArtifactPath(string outputDirectory, string? path, bool useRelativePaths)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return useRelativePaths
            ? Path.GetRelativePath(outputDirectory, path).Replace('\\', '/')
            : path;
    }

    private static NativeReferenceReadinessEvidence Readiness(
        string status,
        string reason,
        string requiredAction,
        bool readyForPromotion)
    {
        return new NativeReferenceReadinessEvidence(
            status,
            reason,
            requiredAction,
            readyForPromotion,
            readyForPromotion ? "none" : reason);
    }

    private static ReferenceImageDimensions CropSize(ComponentCropBounds bounds)
    {
        return new ReferenceImageDimensions(bounds.Width, bounds.Height);
    }

    private static ComponentCropBoundsDelta BoundsDelta(ComponentCropBounds nativeReferenceBounds, ComponentCropBounds macRuntimeBounds)
    {
        return new ComponentCropBoundsDelta(
            nativeReferenceBounds.X - macRuntimeBounds.X,
            nativeReferenceBounds.Y - macRuntimeBounds.Y,
            nativeReferenceBounds.Width - macRuntimeBounds.Width,
            nativeReferenceBounds.Height - macRuntimeBounds.Height);
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
