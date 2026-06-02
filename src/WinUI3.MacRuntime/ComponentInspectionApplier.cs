using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record ComponentInspectionDocument(
    string SchemaVersion,
    IReadOnlyList<ComponentInspectionRow> Rows);

public sealed record ComponentInspectionRow(
    string Component,
    string? Target,
    string VisualGrade,
    string NativeQualityGrade,
    string InspectedBy,
    string InspectedDate,
    string NativeReferenceRunId,
    IReadOnlyList<string>? ComparisonArtifactPaths,
    IReadOnlyList<string>? AcceptedGaps,
    string? ToleranceReason,
    string Notes);

public static class ComponentInspectionApplier
{
    private static readonly HashSet<string> FinalGrades = new(StringComparer.Ordinal)
    {
        "good",
        "production-ready"
    };

    public static ComponentEvidenceDocument Apply(
        ComponentEvidenceDocument evidence,
        ComponentInspectionDocument inspection,
        string evidenceDirectory)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceDirectory);

        if (inspection.SchemaVersion != ArtifactSchemas.ComponentInspection)
        {
            throw new InvalidOperationException($"Unsupported component inspection schema '{inspection.SchemaVersion}'.");
        }

        var rows = inspection.Rows.ToDictionary(
            row => ComponentEvidenceBuilder.ComponentKey(row.Component, row.Target),
            row => row,
            StringComparer.Ordinal);
        if (rows.Count != inspection.Rows.Count)
        {
            throw new InvalidOperationException("Component inspection rows must be unique by component and target.");
        }

        var usedRows = new HashSet<string>(StringComparer.Ordinal);
        var updated = evidence.Components
            .Select(component =>
            {
                var key = ComponentEvidenceBuilder.ComponentKey(component.Component, component.Target);
                if (!rows.TryGetValue(key, out var row))
                {
                    return component;
                }

                usedRows.Add(key);
                ValidateRow(component, row, evidenceDirectory);
                return component with
                {
                    VisualGrade = row.VisualGrade,
                    NativeQualityGrade = row.NativeQualityGrade,
                    Inspection = new ComponentInspectionEvidence(
                        row.InspectedBy,
                        row.InspectedDate,
                        row.NativeReferenceRunId,
                        ComparisonArtifactPaths(component, row),
                        row.AcceptedGaps ?? Array.Empty<string>(),
                        row.ToleranceReason,
                        row.Notes)
                };
            })
            .ToArray();

        var unusedRows = rows.Keys.Except(usedRows, StringComparer.Ordinal).ToArray();
        if (unusedRows.Length != 0)
        {
            throw new InvalidOperationException($"Component inspection row did not match evidence: {unusedRows[0]}.");
        }

        return evidence with { Components = updated };
    }

    public static ComponentEvidenceDocument Apply(string evidencePath, string inspectionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidencePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(inspectionPath);

        using var evidenceStream = File.OpenRead(evidencePath);
        using var inspectionStream = File.OpenRead(inspectionPath);
        var evidence = JsonSerializer.Deserialize<ComponentEvidenceDocument>(evidenceStream, JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Could not read component evidence from '{evidencePath}'.");
        var inspection = JsonSerializer.Deserialize<ComponentInspectionDocument>(inspectionStream, JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Could not read component inspection from '{inspectionPath}'.");
        return Apply(evidence, inspection, Path.GetDirectoryName(Path.GetFullPath(evidencePath))!);
    }

    private static void ValidateRow(ComponentEvidenceEntry component, ComponentInspectionRow row, string evidenceDirectory)
    {
        RequireFinalGrade(row.VisualGrade, "visualGrade");
        RequireFinalGrade(row.NativeQualityGrade, "nativeQualityGrade");
        RequireText(row.InspectedBy, "inspectedBy");
        RequireText(row.InspectedDate, "inspectedDate");
        RequireText(row.NativeReferenceRunId, "nativeReferenceRunId");
        RequireText(row.Notes, "notes");
        if (!DateTimeOffset.TryParse(row.InspectedDate, out _))
        {
            throw new InvalidOperationException("inspectedDate must be parseable as a date or timestamp.");
        }

        var crop = component.Crop ?? throw new InvalidOperationException($"{component.Component} is missing crop evidence.");
        RequireText(crop.NativeReferencePath, "nativeReferencePath");
        RequireText(crop.MacRuntimePath, "macRuntimePath");
        RequireText(crop.PixelDiffPath, "pixelDiffPath");
        if (crop.ChangedPixelPercentage is null ||
            crop.MeanAbsoluteError is null ||
            crop.RootMeanSquaredError is null)
        {
            throw new InvalidOperationException($"{component.Component} is missing component diff metrics.");
        }

        var provenance = crop.NativeReferenceProvenance
            ?? throw new InvalidOperationException($"{component.Component} is missing native reference provenance.");
        if (string.IsNullOrWhiteSpace(provenance.WorkflowRunId) ||
            provenance.WorkflowRunId != row.NativeReferenceRunId)
        {
            throw new InvalidOperationException($"{component.Component} inspection nativeReferenceRunId must match crop provenance.");
        }

        foreach (var path in ComparisonArtifactPaths(component, row))
        {
            var resolved = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(evidenceDirectory, path));
            if (!File.Exists(resolved))
            {
                throw new InvalidOperationException($"{component.Component} inspection artifact path does not exist: {path}.");
            }
        }
    }

    private static IReadOnlyList<string> ComparisonArtifactPaths(ComponentEvidenceEntry component, ComponentInspectionRow row)
    {
        if (row.ComparisonArtifactPaths is { Count: > 0 })
        {
            return row.ComparisonArtifactPaths;
        }

        var crop = component.Crop ?? throw new InvalidOperationException($"{component.Component} is missing crop evidence.");
        return new[]
        {
            crop.NativeReferencePath!,
            crop.MacRuntimePath!,
            crop.PixelDiffPath!
        };
    }

    private static void RequireFinalGrade(string value, string fieldName)
    {
        if (!FinalGrades.Contains(value))
        {
            throw new InvalidOperationException($"{fieldName} must be good or production-ready.");
        }
    }

    private static void RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }
    }
}
