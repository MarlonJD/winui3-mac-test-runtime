using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record NativeReferenceReadinessDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Policy,
    NativeReferenceReadinessTotals Totals,
    IReadOnlyList<NativeReferenceReadinessRow> Rows);

public sealed record NativeReferenceReadinessTotals(
    int RowCount,
    int ReadyRowCount,
    int BlockingRowCount,
    IReadOnlyDictionary<string, int> StatusCounts);

public sealed record NativeReferenceReadinessRow(
    string ScenarioName,
    string EvidencePath,
    string Component,
    string? Target,
    string NativeReferenceStatus,
    string Reason,
    string RequiredAction);

public static class NativeReferenceReadinessBuilder
{
    private const string Policy = "Native Windows screenshots are the visual source of truth. A public row is release-ready only when nativeReferenceStatus is ready; crop presence alone is not sufficient.";

    private static readonly HashSet<string> PreservedSemanticBlockers = new(StringComparer.Ordinal)
    {
        "diagnostic-reference",
        "diagnostic-reference-needs-crop-bounds",
        "native-not-rendered",
        "native-unavailable",
        "offscreen-reference",
        "placeholder-reference",
        "state-reference-incomplete"
    };

    public static NativeReferenceReadinessDocument BuildFromPublicEvidence(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var root = Path.GetFullPath(repositoryRoot);
        var existingRows = LoadExistingRows(root);
        var rows = PublicEvidenceDiscovery.FindCanonicalEvidenceFiles(root)
            .SelectMany(path => RowsForEvidence(root, path, existingRows))
            .ToArray();
        var statusCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            statusCounts[row.NativeReferenceStatus] = statusCounts.TryGetValue(row.NativeReferenceStatus, out var count)
                ? count + 1
                : 1;
        }

        var readyRows = rows.Count(row => string.Equals(row.NativeReferenceStatus, "ready", StringComparison.Ordinal));
        return new NativeReferenceReadinessDocument(
            "0.1",
            DateTimeOffset.UnixEpoch,
            Policy,
            new NativeReferenceReadinessTotals(
                rows.Length,
                readyRows,
                rows.Length - readyRows,
                statusCounts),
            rows);
    }

    public static NativeReferenceReadinessDocument Write(string repositoryRoot)
    {
        var document = BuildFromPublicEvidence(repositoryRoot);
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "native-reference-readiness.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(document, JsonDefaults.Options));
        return document;
    }

    private static IEnumerable<NativeReferenceReadinessRow> RowsForEvidence(
        string repositoryRoot,
        string evidencePath,
        IReadOnlyDictionary<string, NativeReferenceReadinessRow> existingRows)
    {
        var evidence = JsonSerializer.Deserialize<ComponentEvidenceDocument>(File.ReadAllText(evidencePath), JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Could not read component evidence from '{evidencePath}'.");
        var relativeEvidencePath = Path.GetRelativePath(repositoryRoot, evidencePath).Replace('\\', '/');

        foreach (var component in evidence.Components)
        {
            var key = RowKey(evidence.ScenarioName, component.Component, component.Target);
            existingRows.TryGetValue(key, out var existingRow);
            var currentRow = RowForComponent(evidence.ScenarioName, relativeEvidencePath, component);
            if (existingRow is not null &&
                PreservedSemanticBlockers.Contains(existingRow.NativeReferenceStatus) &&
                !string.Equals(currentRow.NativeReferenceStatus, "ready", StringComparison.Ordinal))
            {
                yield return existingRow with { EvidencePath = relativeEvidencePath };
                continue;
            }

            yield return currentRow;
        }
    }

    private static NativeReferenceReadinessRow RowForComponent(
        string scenarioName,
        string evidencePath,
        ComponentEvidenceEntry component)
    {
        if (component.Crop is not { } crop)
        {
            return new NativeReferenceReadinessRow(
                scenarioName,
                evidencePath,
                component.Component,
                component.Target,
                "missing-native-reference-crop",
                "No native reference crop is available.",
                "Capture a native Windows reference with exported target bounds.");
        }

        var status = crop.NativeReferenceReadiness.Status;
        var reason = crop.NativeReferenceReadiness.Reason;
        var requiredAction = crop.NativeReferenceReadiness.RequiredAction;
        var hasTrustedNativeSource = crop.NativeReferenceBounds is not null &&
            !string.IsNullOrWhiteSpace(crop.NativeReferencePath) &&
            string.Equals(crop.NativeReferenceBoundsSource, "windows-native-element-bounds", StringComparison.Ordinal) &&
            crop.NativeReferenceTarget is not null &&
            crop.NativeReferenceProvenance is { ReferenceSource: "native-winui" };
        if (hasTrustedNativeSource)
        {
            status = "ready";
            reason = "Native crop uses Windows native element bounds from native-reference-targets.json.";
            requiredAction = "Keep the native target export with the Windows reference artifact.";
        }

        return new NativeReferenceReadinessRow(
            scenarioName,
            evidencePath,
            component.Component,
            component.Target,
            status,
            reason,
            requiredAction);
    }

    private static IReadOnlyDictionary<string, NativeReferenceReadinessRow> LoadExistingRows(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "native-reference-readiness.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, NativeReferenceReadinessRow>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return new Dictionary<string, NativeReferenceReadinessRow>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, NativeReferenceReadinessRow>(StringComparer.Ordinal);
        foreach (var row in rows.EnumerateArray())
        {
            var scenarioName = ReadString(row, "scenarioName");
            var component = ReadString(row, "component");
            if (string.IsNullOrWhiteSpace(scenarioName) || string.IsNullOrWhiteSpace(component))
            {
                continue;
            }

            var target = ReadString(row, "target");
            var readinessRow = new NativeReferenceReadinessRow(
                scenarioName,
                ReadString(row, "evidencePath") ?? string.Empty,
                component,
                target,
                ReadString(row, "nativeReferenceStatus") ?? "needs-native-crop-bounds",
                ReadString(row, "reason") ?? "Native reference source readiness is not proven.",
                ReadString(row, "requiredAction") ?? "Capture Windows native element bounds and regenerate evidence.");
            result[RowKey(scenarioName, component, target)] = readinessRow;
        }

        return result;
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string RowKey(string scenarioName, string component, string? target)
    {
        return $"{scenarioName}\u001f{component}\u001f{target ?? string.Empty}";
    }
}
