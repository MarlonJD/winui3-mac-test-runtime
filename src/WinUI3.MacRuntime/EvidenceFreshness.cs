using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record EvidenceFreshnessResult(
    string Name,
    string Status,
    IReadOnlyList<string> Problems,
    IReadOnlyList<string> ArtifactPaths)
{
    public bool Passed => Status == "passed";
}

public static class EvidenceFreshness
{
    private const double MetricTolerance = 0.000001;

    public static EvidenceFreshnessResult CheckComponentQualityDashboard(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "component-quality-dashboard.json");
        var artifacts = new[] { RelativePath(repositoryRoot, path) };
        if (!File.Exists(path))
        {
            return Failed(
                "component-quality-dashboard",
                artifacts,
                $"{artifacts[0]} is missing; regenerate with 'winui3-mac-runner component-quality-dashboard'.");
        }

        var expected = JsonSerializer.Serialize(ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot), JsonDefaults.Options);
        var actual = File.ReadAllText(path);
        return NormalizeJson(actual) == NormalizeJson(expected)
            ? Passed("component-quality-dashboard", artifacts)
            : Failed(
                "component-quality-dashboard",
                artifacts,
                $"{artifacts[0]} is stale relative to checked-in component-evidence.json files.");
    }

    public static EvidenceFreshnessResult CheckStateCoverageMatrix(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var path = Path.Combine(repositoryRoot, StateCoverageMatrixBuilder.DefaultArtifactPath);
        var artifacts = new[] { RelativePath(repositoryRoot, path) };
        if (!File.Exists(path))
        {
            return Failed(
                "state-coverage-matrix",
                artifacts,
                $"{artifacts[0]} is missing; regenerate with 'winui3-mac-runner state-coverage-matrix'.");
        }

        var expected = JsonSerializer.Serialize(StateCoverageMatrixBuilder.Build(repositoryRoot), JsonDefaults.Options);
        var actual = File.ReadAllText(path);
        return NormalizeJson(actual) == NormalizeJson(expected)
            ? Passed("state-coverage-matrix", artifacts)
            : Failed(
                "state-coverage-matrix",
                artifacts,
                $"{artifacts[0]} is stale relative to productionStateCoverage and checked-in component evidence.");
    }

    public static EvidenceFreshnessResult CheckNativeQualityFamilyTranches(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var path = Path.Combine(repositoryRoot, NativeQualityFamilyTrancheBuilder.DefaultArtifactPath);
        var artifacts = new[] { RelativePath(repositoryRoot, path) };
        if (!File.Exists(path))
        {
            return Failed(
                "native-quality-family-tranches",
                artifacts,
                $"{artifacts[0]} is missing; regenerate with 'winui3-mac-runner native-quality-family-tranches'.");
        }

        var expected = JsonSerializer.Serialize(NativeQualityFamilyTrancheBuilder.Build(repositoryRoot), JsonDefaults.Options);
        var actual = File.ReadAllText(path);
        return NormalizeJson(actual) == NormalizeJson(expected)
            ? Passed("native-quality-family-tranches", artifacts)
            : Failed(
                "native-quality-family-tranches",
                artifacts,
                $"{artifacts[0]} is stale relative to component-quality dashboard and state coverage matrix inputs.");
    }

    public static EvidenceFreshnessResult CheckVisualDriftDashboard(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "visual-drift-dashboard.json");
        var artifacts = new List<string> { RelativePath(repositoryRoot, path) };
        var problems = new List<string>();
        if (!File.Exists(path))
        {
            return Failed(
                "visual-drift-dashboard",
                artifacts,
                $"{artifacts[0]} is missing.");
        }

        using var dashboard = JsonDocument.Parse(File.ReadAllText(path));
        var root = dashboard.RootElement;
        if (ReadString(root, "gatedMetric") != "component-crop")
        {
            problems.Add("visual-drift-dashboard.json must gate component-crop drift.");
        }

        if (ReadString(root, "informationalMetric") != "whole-screen")
        {
            problems.Add("visual-drift-dashboard.json must keep whole-screen drift informational.");
        }

        if (!root.TryGetProperty("families", out var families) || families.ValueKind != JsonValueKind.Array)
        {
            problems.Add("visual-drift-dashboard.json must declare a families array.");
        }
        else
        {
            foreach (var family in families.EnumerateArray())
            {
                CheckVisualDriftFamily(repositoryRoot, family, artifacts, problems);
            }
        }

        return problems.Count == 0
            ? Passed("visual-drift-dashboard", artifacts)
            : Failed("visual-drift-dashboard", artifacts, problems);
    }

    private static void CheckVisualDriftFamily(
        string repositoryRoot,
        JsonElement family,
        List<string> artifacts,
        List<string> problems)
    {
        var familyName = ReadString(family, "family") ?? "unknown family";
        if (!family.TryGetProperty("componentCropDrift", out var componentCrop) ||
            componentCrop.ValueKind != JsonValueKind.Object ||
            !componentCrop.TryGetProperty("gated", out var cropGated) ||
            cropGated.ValueKind != JsonValueKind.True)
        {
            problems.Add($"{familyName}: component-crop drift must be gated.");
        }

        if (!family.TryGetProperty("wholeScreenDrift", out var wholeScreen) ||
            wholeScreen.ValueKind != JsonValueKind.Object)
        {
            problems.Add($"{familyName}: whole-screen drift is missing.");
            return;
        }

        if (!wholeScreen.TryGetProperty("gated", out var wholeGated) ||
            wholeGated.ValueKind != JsonValueKind.False)
        {
            problems.Add($"{familyName}: whole-screen drift must stay informational.");
        }

        var pixelDiffRelative = ReadString(family, "pixelDiffPath");
        if (string.IsNullOrWhiteSpace(pixelDiffRelative))
        {
            problems.Add($"{familyName}: pixelDiffPath is missing.");
            return;
        }

        artifacts.Add(pixelDiffRelative);
        var pixelDiffPath = Path.Combine(repositoryRoot, pixelDiffRelative);
        if (!File.Exists(pixelDiffPath))
        {
            problems.Add($"{familyName}: {pixelDiffRelative} is missing.");
            return;
        }

        using var pixelDiff = JsonDocument.Parse(File.ReadAllText(pixelDiffPath));
        if (!pixelDiff.RootElement.TryGetProperty("changedPixelPercentage", out var expectedElement) ||
            expectedElement.ValueKind != JsonValueKind.Number ||
            !wholeScreen.TryGetProperty("changedPixelPercentage", out var actualElement) ||
            actualElement.ValueKind != JsonValueKind.Number)
        {
            problems.Add($"{familyName}: changedPixelPercentage is missing from visual-drift-dashboard.json or {pixelDiffRelative}.");
            return;
        }

        var expected = expectedElement.GetDouble();
        var actual = actualElement.GetDouble();
        if (Math.Abs(expected - actual) > MetricTolerance)
        {
            problems.Add(
                $"{familyName}: visual-drift-dashboard.json whole-screen changedPixelPercentage {actual.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)} does not match {pixelDiffRelative} value {expected.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)}.");
        }
    }

    private static EvidenceFreshnessResult Passed(string name, IReadOnlyList<string> artifacts)
    {
        return new EvidenceFreshnessResult(name, "passed", Array.Empty<string>(), artifacts.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static EvidenceFreshnessResult Failed(string name, IReadOnlyList<string> artifacts, params string[] problems)
    {
        return Failed(name, artifacts, (IReadOnlyList<string>)problems);
    }

    private static EvidenceFreshnessResult Failed(string name, IReadOnlyList<string> artifacts, IReadOnlyList<string> problems)
    {
        return new EvidenceFreshnessResult(name, "failed", problems, artifacts.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string RelativePath(string repositoryRoot, string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static string NormalizeJson(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
    }
}
