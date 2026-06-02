using System.Text.Json;

namespace WinUI3.MacRuntime;

public static class ComponentInspectionTemplate
{
    public const string DefaultFileName = "component-inspection-template.json";
    private const string TodoFinalGrade = "TODO-good-or-production-ready";

    public static ComponentInspectionDocument Build(ComponentEvidenceDocument evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var rows = evidence.Components
            .Where(component => component.Inspection is null)
            .Select(BuildRow)
            .ToArray();
        return new ComponentInspectionDocument(
            SchemaVersion: ArtifactSchemas.ComponentInspection,
            Rows: rows);
    }

    public static ComponentInspectionDocument Build(string evidencePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidencePath);

        using var stream = File.OpenRead(evidencePath);
        var evidence = JsonSerializer.Deserialize<ComponentEvidenceDocument>(stream, JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Could not read component evidence from '{evidencePath}'.");
        return Build(evidence);
    }

    private static ComponentInspectionRow BuildRow(ComponentEvidenceEntry component)
    {
        var crop = component.Crop;
        var comparisonArtifactPaths = new[]
        {
            crop?.NativeReferencePath,
            crop?.MacRuntimePath,
            crop?.PixelDiffPath
        }.Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => path!).ToArray();

        return new ComponentInspectionRow(
            Component: component.Component,
            Target: component.Target,
            VisualGrade: TodoFinalGrade,
            NativeQualityGrade: TodoFinalGrade,
            InspectedBy: "TODO-reviewer",
            InspectedDate: "TODO-YYYY-MM-DD",
            NativeReferenceRunId: crop?.NativeReferenceProvenance?.WorkflowRunId ?? "TODO-native-reference-run-id",
            ComparisonArtifactPaths: comparisonArtifactPaths,
            AcceptedGaps: Array.Empty<string>(),
            ToleranceReason: null,
            Notes: "TODO: inspect native, macOS, and diff crops before choosing final grades.");
    }
}
