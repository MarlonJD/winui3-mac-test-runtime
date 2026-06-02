using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record NativeReferenceImportDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string SourceRoot,
    string OutputRoot,
    int ExpectedComponentScenarioCount,
    int ImportedReferenceCount,
    IReadOnlyList<string> MissingComponentScenarioPaths,
    IReadOnlyList<string> Problems,
    IReadOnlyList<NativeReferenceImportItem> Items,
    string Status);

public sealed record NativeReferenceImportItem(
    string ScenarioName,
    string ScenarioPath,
    string? FixtureProjectPath,
    string SourceDirectory,
    string ReferenceImagePath,
    string ReferenceMetadataPath,
    string ImportedReferenceImagePath,
    string ImportedReferenceMetadataPath,
    string? ReferenceSource,
    string? WorkflowRunId,
    string? CommitSha,
    string? RunnerImage,
    string? Theme,
    string Status);

public static class NativeReferenceImporter
{
    public static string? ResolveReferenceImagePath(
        string repositoryRoot,
        string? referencePath,
        string? scenarioName,
        string? scenarioPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        if (string.IsNullOrWhiteSpace(referencePath))
        {
            return null;
        }

        var reference = Path.GetFullPath(referencePath);
        if (File.Exists(reference))
        {
            return reference;
        }

        if (!Directory.Exists(reference))
        {
            throw new FileNotFoundException("Reference image or directory was not found.", reference);
        }

        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            throw new InvalidOperationException("A scenario is required when --reference points to a native reference directory.");
        }

        var normalizedScenarioPath = NormalizeScenarioPath(repositoryRoot, scenarioPath);
        var manifestPath = Path.Combine(reference, "native-reference-import.json");
        if (File.Exists(manifestPath))
        {
            var manifest = JsonSerializer.Deserialize<NativeReferenceImportDocument>(File.ReadAllText(manifestPath), JsonDefaults.Options)
                ?? throw new InvalidOperationException($"Native reference import manifest '{manifestPath}' did not contain a valid JSON object.");
            var matches = manifest.Items
                .Where(item => string.Equals(item.ReferenceSource, "native-winui", StringComparison.Ordinal))
                .Where(item => string.Equals(item.ScenarioName, scenarioName, StringComparison.Ordinal) ||
                    (!string.IsNullOrWhiteSpace(normalizedScenarioPath) &&
                        string.Equals(item.ScenarioPath, normalizedScenarioPath, StringComparison.Ordinal)))
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Native reference directory '{reference}' contains multiple references for scenario '{scenarioName}'.");
            }

            if (matches.Length == 1)
            {
                var imagePath = Path.Combine(reference, matches[0].ImportedReferenceImagePath);
                if (!File.Exists(imagePath))
                {
                    throw new FileNotFoundException("Imported native reference image from manifest was not found.", imagePath);
                }

                return imagePath;
            }
        }

        var candidates = new[]
        {
            Path.Combine(reference, scenarioName, "windows-reference.png"),
            Path.Combine(reference, "windows-reference.png")
        };
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate) && ReferenceMatchesScenario(candidate, scenarioName, normalizedScenarioPath))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"No native WinUI reference image for scenario '{scenarioName}' was found in '{reference}'.");
    }

    public static NativeReferenceImportDocument Import(
        string repositoryRoot,
        string sourceRoot,
        string outputRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var repository = Path.GetFullPath(repositoryRoot);
        var source = Path.GetFullPath(sourceRoot);
        var output = Path.GetFullPath(outputRoot);
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Native reference source directory was not found: {source}");
        }

        Directory.CreateDirectory(output);
        var expectedComponentScenarios = Directory
            .EnumerateFiles(Path.Combine(repository, "fixtures", "ComponentParityLab.WinUI", "scenarios"), "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => RelativePath(repository, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var expectedSet = expectedComponentScenarios.ToHashSet(StringComparer.Ordinal);

        var items = new List<NativeReferenceImportItem>();
        var problems = new List<string>();
        foreach (var metadataPath in Directory.EnumerateFiles(source, "windows-reference.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            var sourceDirectory = Path.GetDirectoryName(metadataPath)!;
            var imagePath = Path.Combine(sourceDirectory, "windows-reference.png");
            if (!File.Exists(imagePath))
            {
                problems.Add($"Missing windows-reference.png beside {RelativePath(source, metadataPath)}.");
                continue;
            }

            NativeReferenceProvenance? provenance;
            try
            {
                provenance = JsonSerializer.Deserialize<NativeReferenceProvenance>(File.ReadAllText(metadataPath), JsonDefaults.Options);
            }
            catch (JsonException exception)
            {
                problems.Add($"Could not parse {RelativePath(source, metadataPath)}: {exception.Message}");
                continue;
            }

            if (provenance is null)
            {
                problems.Add($"Could not parse {RelativePath(source, metadataPath)}.");
                continue;
            }

            var scenarioPath = NormalizeRelativePath(provenance.ScenarioPath);
            var scenarioName = string.IsNullOrWhiteSpace(provenance.ScenarioName)
                ? Path.GetFileName(sourceDirectory)
                : provenance.ScenarioName!;
            var itemProblems = ValidateProvenance(provenance, scenarioPath, scenarioName, repository);
            problems.AddRange(itemProblems.Select(problem => $"{scenarioName}: {problem}"));

            var importedDirectory = Path.Combine(output, SafeName(scenarioName));
            Directory.CreateDirectory(importedDirectory);
            var importedImagePath = Path.Combine(importedDirectory, "windows-reference.png");
            var importedMetadataPath = Path.Combine(importedDirectory, "windows-reference.json");
            File.Copy(imagePath, importedImagePath, overwrite: true);
            File.Copy(metadataPath, importedMetadataPath, overwrite: true);

            items.Add(new NativeReferenceImportItem(
                ScenarioName: scenarioName,
                ScenarioPath: scenarioPath ?? string.Empty,
                FixtureProjectPath: NormalizeRelativePath(provenance.FixtureProjectPath),
                SourceDirectory: RelativePath(source, sourceDirectory),
                ReferenceImagePath: RelativePath(source, imagePath),
                ReferenceMetadataPath: RelativePath(source, metadataPath),
                ImportedReferenceImagePath: RelativePath(output, importedImagePath),
                ImportedReferenceMetadataPath: RelativePath(output, importedMetadataPath),
                ReferenceSource: provenance.ReferenceSource,
                WorkflowRunId: provenance.WorkflowRunId,
                CommitSha: provenance.CommitSha,
                RunnerImage: provenance.RunnerImage,
                Theme: provenance.Theme,
                Status: itemProblems.Count == 0 ? "imported" : "imported-with-problems"));
        }

        var importedComponentScenarios = items
            .Where(item => string.Equals(item.ReferenceSource, "native-winui", StringComparison.Ordinal))
            .Select(item => item.ScenarioPath)
            .Where(path => expectedSet.Contains(path))
            .ToHashSet(StringComparer.Ordinal);
        var missingComponentScenarios = expectedComponentScenarios
            .Where(path => !importedComponentScenarios.Contains(path))
            .ToArray();
        if (missingComponentScenarios.Length > 0)
        {
            problems.Add($"Missing native references for {missingComponentScenarios.Length} component parity scenario(s).");
        }

        var document = new NativeReferenceImportDocument(
            SchemaVersion: ArtifactSchemas.NativeReferenceImport,
            GeneratedAt: DateTimeOffset.UnixEpoch,
            SourceRoot: source,
            OutputRoot: output,
            ExpectedComponentScenarioCount: expectedComponentScenarios.Length,
            ImportedReferenceCount: items.Count,
            MissingComponentScenarioPaths: missingComponentScenarios,
            Problems: problems,
            Items: items.OrderBy(item => item.ScenarioName, StringComparer.Ordinal).ToArray(),
            Status: problems.Count == 0 ? "passed" : "failed");

        File.WriteAllText(
            Path.Combine(output, "native-reference-import.json"),
            JsonSerializer.Serialize(document, JsonDefaults.Options));
        return document;
    }

    private static IReadOnlyList<string> ValidateProvenance(
        NativeReferenceProvenance provenance,
        string? scenarioPath,
        string scenarioName,
        string repositoryRoot)
    {
        var problems = new List<string>();
        if (!string.Equals(provenance.ReferenceSource, "native-winui", StringComparison.Ordinal))
        {
            problems.Add($"referenceSource is '{provenance.ReferenceSource ?? "missing"}', expected native-winui.");
        }

        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            problems.Add("scenarioPath is missing.");
        }
        else if (!File.Exists(Path.Combine(repositoryRoot, scenarioPath)))
        {
            problems.Add($"scenarioPath '{scenarioPath}' does not exist.");
        }

        if (string.IsNullOrWhiteSpace(provenance.ScenarioName))
        {
            problems.Add("scenarioName is missing.");
        }
        else if (!string.Equals(provenance.ScenarioName, scenarioName, StringComparison.Ordinal))
        {
            problems.Add($"scenarioName '{provenance.ScenarioName}' does not match import directory '{scenarioName}'.");
        }

        if (string.IsNullOrWhiteSpace(provenance.FixtureProjectPath))
        {
            problems.Add("fixtureProjectPath is missing.");
        }
        else if (!File.Exists(Path.Combine(repositoryRoot, NormalizeRelativePath(provenance.FixtureProjectPath)!)))
        {
            problems.Add($"fixtureProjectPath '{provenance.FixtureProjectPath}' does not exist.");
        }

        if (string.IsNullOrWhiteSpace(provenance.CommitSha))
        {
            problems.Add("commitSha is missing.");
        }

        if (string.IsNullOrWhiteSpace(provenance.WorkflowRunId))
        {
            problems.Add("workflowRunId is missing.");
        }

        if (string.IsNullOrWhiteSpace(provenance.RunnerImage))
        {
            problems.Add("runnerImage is missing.");
        }

        if (provenance.Viewport is null)
        {
            problems.Add("viewport is missing.");
        }

        if (provenance.Scale is null)
        {
            problems.Add("scale is missing.");
        }

        if (string.IsNullOrWhiteSpace(provenance.Theme))
        {
            problems.Add("theme is missing.");
        }

        if (string.IsNullOrWhiteSpace(provenance.CaptureMode))
        {
            problems.Add("captureMode is missing.");
        }

        return problems;
    }

    private static string SafeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
    }

    private static string? NormalizeRelativePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : path.Replace('\\', '/');
    }

    private static string? NormalizeScenarioPath(string repositoryRoot, string? scenarioPath)
    {
        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            return null;
        }

        var normalized = NormalizeRelativePath(scenarioPath)!;
        if (!Path.IsPathFullyQualified(normalized))
        {
            return normalized;
        }

        var repository = Path.GetFullPath(repositoryRoot);
        var fullPath = Path.GetFullPath(scenarioPath);
        return RelativePath(repository, fullPath);
    }

    private static bool ReferenceMatchesScenario(
        string imagePath,
        string scenarioName,
        string? scenarioPath)
    {
        var metadataPath = Path.Combine(Path.GetDirectoryName(imagePath)!, "windows-reference.json");
        if (!File.Exists(metadataPath))
        {
            return false;
        }

        var provenance = JsonSerializer.Deserialize<NativeReferenceProvenance>(File.ReadAllText(metadataPath), JsonDefaults.Options);
        if (provenance is null ||
            !string.Equals(provenance.ReferenceSource, "native-winui", StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(provenance.ScenarioName, scenarioName, StringComparison.Ordinal))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(scenarioPath) &&
            string.Equals(NormalizeRelativePath(provenance.ScenarioPath), scenarioPath, StringComparison.Ordinal);
    }

    private static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }
}
