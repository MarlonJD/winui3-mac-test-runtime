using System.Text.Json;

namespace WinUI3.MacRuntime;

/// <summary>
/// Sanitized, checked-in rollup of the downstream Windows native visual parity baseline.
/// It keeps the eight downstream probe scenarios auditable and reviewable without copying
/// private Windows screenshots or pixel-diff PNGs into the runtime repository. The actual
/// PNG evidence stays under the operator's private QA evidence root.
/// </summary>
public sealed record DownstreamNativeVisualParityAudit(
    string SchemaVersion,
    string AuditDate,
    string ReferenceCaptureDate,
    string ReferenceProvenance,
    string EvidenceFormat,
    string EvidenceBoundary,
    DownstreamNativeParityViewport Viewport,
    string MaxChannelDeltaPolicy,
    IReadOnlyList<DownstreamNativeParityScenario> Scenarios,
    IReadOnlyList<DownstreamNativeParitySharedGap> SharedGaps,
    IReadOnlyList<DownstreamNativeParityLadderLevel> ThresholdLadder,
    IReadOnlyList<string> ManualInspectionCriteria)
{
    public static DownstreamNativeVisualParityAudit Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<DownstreamNativeVisualParityAudit>(stream, JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Unable to read downstream native visual parity audit from {path}.");
    }

    /// <summary>
    /// Classifies a single comparison result against the documented L0..L5 threshold ladder.
    /// Returns the highest ladder level whose whole-image changed-pixel, MAE, and RMSE bars are
    /// all satisfied. The L5 promotion bar has separate broad (app route) and focused
    /// (command/status/form route) threshold sets; pass <paramref name="focused"/> to evaluate a
    /// focused route against the tighter focused thresholds.
    /// </summary>
    public static string ClassifyLadder(
        double changedPixelPercentage,
        double meanAbsoluteError,
        double rootMeanSquaredError,
        bool focused = false)
    {
        // L5 Premium production promotion.
        if (focused)
        {
            if (Within(changedPixelPercentage, 24d) && Within(meanAbsoluteError, 5.5d) && Within(rootMeanSquaredError, 20d))
            {
                return "L5";
            }
        }
        else if (Within(changedPixelPercentage, 35d) && Within(meanAbsoluteError, 6.5d) && Within(rootMeanSquaredError, 24d))
        {
            return "L5";
        }

        // L4 Conservative native-comparison pass.
        if (Within(changedPixelPercentage, 45d) && Within(meanAbsoluteError, 8d) && Within(rootMeanSquaredError, 28d))
        {
            return "L4";
        }

        // L3 Control-family parity.
        if (Within(changedPixelPercentage, 55d) && Within(meanAbsoluteError, 8d) && Within(rootMeanSquaredError, 28d))
        {
            return "L3";
        }

        // L2 Layout and density parity.
        if (Within(changedPixelPercentage, 70d) && Within(meanAbsoluteError, 10d) && Within(rootMeanSquaredError, 32d))
        {
            return "L2";
        }

        // L1 Coarse route alignment.
        if (Within(changedPixelPercentage, 90d) && Within(meanAbsoluteError, 12d) && Within(rootMeanSquaredError, 36d))
        {
            return "L1";
        }

        return "L0";
    }

    /// <summary>
    /// Regenerates scenario rollup rows from freshly parsed pixel-diff metrics, classifying each
    /// scenario against the ladder using the route-appropriate focused/broad threshold set. This
    /// keeps the checked-in baseline reproducible once real Windows references are re-staged.
    /// </summary>
    public static IReadOnlyList<DownstreamNativeParityScenario> RollupFromProbeMetrics(
        IEnumerable<DownstreamNativeParityProbeMetric> metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics
            .OrderBy(metric => metric.Priority)
            .Select(metric => new DownstreamNativeParityScenario(
                metric.Scenario,
                metric.Priority,
                metric.Route,
                metric.Width,
                metric.Height,
                new DownstreamNativeParityMetrics(
                    metric.ChangedPixelPercentage,
                    metric.MeanAbsoluteError,
                    metric.RootMeanSquaredError,
                    metric.ThresholdStatus,
                    metric.ThresholdFailure,
                    metric.ArtifactStatus,
                    metric.FontProvenanceStatus,
                    metric.ImageIntegrityStatus,
                    ClassifyLadder(
                        metric.ChangedPixelPercentage,
                        metric.MeanAbsoluteError,
                        metric.RootMeanSquaredError,
                        IsFocusedRoute(metric.Scenario))),
                metric.RouteSelectionState,
                metric.MainVisualGaps))
            .ToArray();
    }

    /// <summary>
    /// Command, status, and form routes are graded against the tighter focused L5 bar; broad
    /// shell/list/detail app routes use the broad L5 bar.
    /// </summary>
    public static bool IsFocusedRoute(string scenario) =>
        scenario is "login-light"
            or "status-states-light"
            or "command-search-light"
            or "settings-profile-light";

    private static bool Within(double value, double threshold) => value <= threshold + 1e-9;
}

public sealed record DownstreamNativeParityViewport(int Width, int Height);

public sealed record DownstreamNativeParityMetrics(
    double ChangedPixelPercentage,
    double MeanAbsoluteError,
    double RootMeanSquaredError,
    string ThresholdStatus,
    string ThresholdFailure,
    string ArtifactStatus,
    string FontProvenanceStatus,
    string ImageIntegrityStatus,
    string LadderLevel);

public sealed record DownstreamNativeParityScenario(
    string Scenario,
    int Priority,
    string Route,
    int Width,
    int Height,
    DownstreamNativeParityMetrics Baseline,
    string RouteSelectionState,
    IReadOnlyList<string> MainVisualGaps);

public sealed record DownstreamNativeParitySharedGap(string Category, string Gap, string Evidence);

public sealed record DownstreamNativeParityLadderLevel(
    string Ladder,
    string Purpose,
    string ChangedPixels,
    string Mae,
    string Rmse,
    string ManualGate);

public sealed record DownstreamNativeParityProbeMetric(
    string Scenario,
    int Priority,
    string Route,
    int Width,
    int Height,
    double ChangedPixelPercentage,
    double MeanAbsoluteError,
    double RootMeanSquaredError,
    string ThresholdStatus,
    string ThresholdFailure,
    string ArtifactStatus,
    string FontProvenanceStatus,
    string ImageIntegrityStatus,
    string RouteSelectionState,
    IReadOnlyList<string> MainVisualGaps);
