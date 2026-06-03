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
    string? ReferenceTargetsPath,
    string ImportedReferenceImagePath,
    string ImportedReferenceMetadataPath,
    string? ImportedReferenceTargetsPath,
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
        var expectedComponentScenarios = ExpectedPublicScenarioPaths(repository)
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
            var targetsPath = Path.Combine(sourceDirectory, "native-reference-targets.json");
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
            var itemProblems = ValidateProvenance(provenance, scenarioPath, scenarioName, repository).ToList();
            NativeReferenceTargetDocument? targets = null;
            if (File.Exists(targetsPath))
            {
                try
                {
                    targets = JsonSerializer.Deserialize<NativeReferenceTargetDocument>(File.ReadAllText(targetsPath), JsonDefaults.Options);
                }
                catch (JsonException exception)
                {
                    itemProblems.Add($"Could not parse native-reference-targets.json: {exception.Message}");
                }

                if (targets is not null)
                {
                    itemProblems.AddRange(ValidateTargets(targets, provenance, scenarioName, scenarioPath, repository));
                    provenance = provenance with
                    {
                        NativeReferenceTargetsPath = "native-reference-targets.json"
                    };
                }
            }
            else if (string.Equals(provenance.ReferenceSource, "native-winui", StringComparison.Ordinal) &&
                IsNativeTargetRequiredScenarioPath(scenarioPath))
            {
                itemProblems.Add("native-reference-targets.json is missing; public component references require Windows native element bounds.");
            }

            problems.AddRange(itemProblems.Select(problem => $"{scenarioName}: {problem}"));

            var importedDirectory = Path.Combine(output, SafeName(scenarioName));
            Directory.CreateDirectory(importedDirectory);
            var importedImagePath = Path.Combine(importedDirectory, "windows-reference.png");
            var importedMetadataPath = Path.Combine(importedDirectory, "windows-reference.json");
            var importedTargetsPath = File.Exists(targetsPath)
                ? Path.Combine(importedDirectory, "native-reference-targets.json")
                : null;
            File.Copy(imagePath, importedImagePath, overwrite: true);
            File.WriteAllText(importedMetadataPath, JsonSerializer.Serialize(provenance, JsonDefaults.Options));
            if (importedTargetsPath is not null)
            {
                File.Copy(targetsPath, importedTargetsPath, overwrite: true);
            }

            items.Add(new NativeReferenceImportItem(
                ScenarioName: scenarioName,
                ScenarioPath: scenarioPath ?? string.Empty,
                FixtureProjectPath: NormalizeRelativePath(provenance.FixtureProjectPath),
                SourceDirectory: RelativePath(source, sourceDirectory),
                ReferenceImagePath: RelativePath(source, imagePath),
                ReferenceMetadataPath: RelativePath(source, metadataPath),
                ReferenceTargetsPath: File.Exists(targetsPath) ? RelativePath(source, targetsPath) : null,
                ImportedReferenceImagePath: RelativePath(output, importedImagePath),
                ImportedReferenceMetadataPath: RelativePath(output, importedMetadataPath),
                ImportedReferenceTargetsPath: importedTargetsPath is null ? null : RelativePath(output, importedTargetsPath),
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
            problems.Add($"Missing native references for {missingComponentScenarios.Length} public component scenario(s).");
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

        if (provenance.Dimensions is not { Width: > 0, Height: > 0 })
        {
            problems.Add("dimensions are missing or invalid.");
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

        if (string.IsNullOrWhiteSpace(provenance.CapturedAt) ||
            !DateTimeOffset.TryParse(provenance.CapturedAt, out _))
        {
            problems.Add("capturedAt is missing or invalid.");
        }

        return problems;
    }

    private static IReadOnlyList<string> ValidateTargets(
        NativeReferenceTargetDocument targets,
        NativeReferenceProvenance provenance,
        string scenarioName,
        string? scenarioPath,
        string repositoryRoot)
    {
        var problems = new List<string>();
        if (!string.Equals(targets.ReferenceSource, "native-winui-element-bounds", StringComparison.Ordinal))
        {
            problems.Add($"native-reference-targets.json referenceSource is '{targets.ReferenceSource ?? "missing"}', expected native-winui-element-bounds.");
        }

        if (!string.Equals(targets.CoordinateSpace, "client-area", StringComparison.Ordinal))
        {
            problems.Add($"native-reference-targets.json coordinateSpace is '{targets.CoordinateSpace ?? "missing"}', expected client-area.");
        }

        if (!string.Equals(targets.ScenarioName, scenarioName, StringComparison.Ordinal))
        {
            problems.Add($"native-reference-targets.json scenarioName '{targets.ScenarioName}' does not match import directory '{scenarioName}'.");
        }

        if (!string.IsNullOrWhiteSpace(scenarioPath) &&
            !string.IsNullOrWhiteSpace(targets.ScenarioPath) &&
            !string.Equals(NormalizeRelativePath(targets.ScenarioPath), scenarioPath, StringComparison.Ordinal))
        {
            problems.Add($"native-reference-targets.json scenarioPath '{targets.ScenarioPath}' does not match reference provenance '{scenarioPath}'.");
        }

        if (string.IsNullOrWhiteSpace(targets.CommitSha))
        {
            problems.Add("native-reference-targets.json commitSha is missing.");
        }
        else if (!string.IsNullOrWhiteSpace(provenance.CommitSha) &&
            !string.Equals(targets.CommitSha, provenance.CommitSha, StringComparison.Ordinal))
        {
            problems.Add("native-reference-targets.json commitSha does not match windows-reference.json.");
        }

        if (string.IsNullOrWhiteSpace(targets.WorkflowRunId))
        {
            problems.Add("native-reference-targets.json workflowRunId is missing.");
        }
        else if (!string.IsNullOrWhiteSpace(provenance.WorkflowRunId) &&
            !string.Equals(targets.WorkflowRunId, provenance.WorkflowRunId, StringComparison.Ordinal))
        {
            problems.Add("native-reference-targets.json workflowRunId does not match windows-reference.json.");
        }

        if (targets.Dimensions is not { Width: > 0, Height: > 0 })
        {
            problems.Add("native-reference-targets.json dimensions are missing or invalid.");
        }

        if (targets.CapturedAt is null)
        {
            problems.Add("native-reference-targets.json capturedAt is missing.");
        }

        var expectedTargets = IsExpectedPublicScenarioPath(repositoryRoot, scenarioPath)
            ? ExpectedScenarioTargets(repositoryRoot, scenarioPath!).ToArray()
            : Array.Empty<ExpectedNativeReferenceTarget>();
        if (expectedTargets.Length > 0 && targets.Targets.Count == 0)
        {
            problems.Add("native-reference-targets.json contains no target bounds.");
        }

        var captureBounds = CaptureBounds(targets, provenance);
        if (expectedTargets.Length > 0 && captureBounds is null)
        {
            problems.Add("native-reference-targets.json must include root bounds, viewport, or screenshot dimensions so target bounds can be validated inside the client area.");
        }

        if (expectedTargets.Length > 0)
        {
            foreach (var expectedTarget in expectedTargets)
            {
                var matches = targets.Targets
                    .Where(target => string.Equals(target.Target, expectedTarget.Target, StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length == 0)
                {
                    problems.Add($"native-reference-targets.json is missing required public row target '{expectedTarget.Target}'.");
                    continue;
                }

                var exactMatches = matches
                    .Where(target => string.Equals(target.Component, expectedTarget.Component, StringComparison.Ordinal))
                    .ToArray();
                var selectedMatches = exactMatches;
                if (exactMatches.Length > 1)
                {
                    problems.Add($"native-reference-targets.json target '{expectedTarget.Target}' for component '{expectedTarget.Component}' maps ambiguously to {exactMatches.Length} native elements.");
                }

                if (exactMatches.Length == 0)
                {
                    var sharedTarget = expectedTargets.Count(target => string.Equals(target.Target, expectedTarget.Target, StringComparison.Ordinal)) > 1;
                    if (sharedTarget && matches.Length == 1)
                    {
                        selectedMatches = matches;
                    }
                    else
                    {
                        var missingComponentIdentity = matches
                            .Where(match => string.IsNullOrWhiteSpace(match.Component))
                            .ToArray();
                        foreach (var _ in missingComponentIdentity)
                        {
                            problems.Add($"native-reference-targets.json target '{expectedTarget.Target}' is missing component identity for expected component '{expectedTarget.Component}'.");
                        }

                        foreach (var match in matches.Where(match => !string.IsNullOrWhiteSpace(match.Component)))
                        {
                            problems.Add($"native-reference-targets.json target '{expectedTarget.Target}' component '{match.Component}' does not match expected public row component '{expectedTarget.Component}'.");
                        }
                    }
                }

                foreach (var match in selectedMatches)
                {
                    if (string.IsNullOrWhiteSpace(match.Target))
                    {
                        problems.Add("native-reference-targets.json contains a target with no identity.");
                    }

                    if (match.Bounds.Width <= 0 || match.Bounds.Height <= 0)
                    {
                        problems.Add($"native-reference-targets.json target '{match.Target}' has non-positive bounds.");
                    }

                    if (captureBounds is not null && !IsInside(match.Bounds, captureBounds))
                    {
                        problems.Add($"native-reference-targets.json target '{match.Target}' has bounds outside the captured client area.");
                    }

                    if (match.ActualSize is not { Width: > 0, Height: > 0 })
                    {
                        problems.Add($"native-reference-targets.json target '{match.Target}' is missing positive actualSize metadata.");
                    }

                    if (string.IsNullOrWhiteSpace(match.BoundsSource))
                    {
                        problems.Add($"native-reference-targets.json target '{match.Target}' is missing boundsSource metadata.");
                    }

                    if (match.CapturedAt is null)
                    {
                        problems.Add($"native-reference-targets.json target '{match.Target}' is missing capturedAt metadata.");
                    }

                    if (IsUntrustworthyElementForComponent(expectedTarget.Component, match.ElementType))
                    {
                        problems.Add($"native-reference-targets.json target '{match.Target}' resolved to '{match.ElementType}', which is not a trustworthy native element for expected public row component '{expectedTarget.Component}'.");
                    }
                }
            }
        }

        return problems;
    }

    private static IEnumerable<string> ExpectedPublicScenarioPaths(string repositoryRoot)
    {
        var scenarioNames = PublicEvidenceDiscovery
            .FindCanonicalEvidenceFiles(repositoryRoot)
            .Select(path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.TryGetProperty("scenarioName", out var scenarioName) &&
                    scenarioName.ValueKind == JsonValueKind.String
                        ? scenarioName.GetString()
                        : null;
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var relativeDirectory in new[]
        {
            Path.Combine("fixtures", "ComponentParityLab.WinUI", "scenarios"),
            Path.Combine("fixtures", "PublicAdminWorkbench.WinUI", "scenarios")
        })
        {
            var directory = Path.Combine(repositoryRoot, relativeDirectory);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (scenarioNames.Contains(Path.GetFileNameWithoutExtension(path)))
                {
                    yield return path;
                }
            }
        }
    }

    private static IReadOnlyList<ExpectedNativeReferenceTarget> ExpectedScenarioTargets(string repositoryRoot, string scenarioPath)
    {
        var fullPath = Path.Combine(repositoryRoot, scenarioPath);
        using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
        if (!document.RootElement.TryGetProperty("requirements", out var requirements) ||
            requirements.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ExpectedNativeReferenceTarget>();
        }

        return requirements
            .EnumerateArray()
            .Select(requirement =>
            {
                var component = requirement.TryGetProperty("component", out var componentProperty) &&
                    componentProperty.ValueKind == JsonValueKind.String
                        ? componentProperty.GetString()
                        : null;
                var target = requirement.TryGetProperty("target", out var targetProperty) &&
                    targetProperty.ValueKind == JsonValueKind.String
                        ? targetProperty.GetString()
                        : null;
                return string.IsNullOrWhiteSpace(component) || string.IsNullOrWhiteSpace(target)
                    ? null
                    : new ExpectedNativeReferenceTarget(component!, target!);
            })
            .Where(target => target is not null)
            .Select(target => target!)
            .Distinct()
            .OrderBy(target => target.Target, StringComparer.Ordinal)
            .ThenBy(target => target.Component, StringComparer.Ordinal)
            .ToArray();
    }

    private static NativeReferenceBounds? CaptureBounds(
        NativeReferenceTargetDocument targets,
        NativeReferenceProvenance provenance)
    {
        if (targets.RootBounds is { Width: > 0, Height: > 0 } rootBounds)
        {
            return rootBounds;
        }

        if (provenance.Dimensions is { Width: > 0, Height: > 0 } dimensions)
        {
            return new NativeReferenceBounds(0, 0, dimensions.Width, dimensions.Height);
        }

        if (targets.Viewport is { Width: > 0, Height: > 0 } targetViewport)
        {
            return new NativeReferenceBounds(0, 0, targetViewport.Width, targetViewport.Height);
        }

        return provenance.Viewport is { Width: > 0, Height: > 0 } provenanceViewport
            ? new NativeReferenceBounds(0, 0, provenanceViewport.Width, provenanceViewport.Height)
            : null;
    }

    private static bool IsInside(NativeReferenceBounds targetBounds, NativeReferenceBounds captureBounds)
    {
        const double Epsilon = 0.01;
        return targetBounds.X + Epsilon >= captureBounds.X &&
            targetBounds.Y + Epsilon >= captureBounds.Y &&
            targetBounds.X + targetBounds.Width <= captureBounds.X + captureBounds.Width + Epsilon &&
            targetBounds.Y + targetBounds.Height <= captureBounds.Y + captureBounds.Height + Epsilon;
    }

    private static bool IsUntrustworthyElementForComponent(string component, string? elementType)
    {
        if (string.IsNullOrWhiteSpace(elementType))
        {
            return true;
        }

        var simpleType = elementType.Split('.').Last();
        if (string.Equals(component, simpleType, StringComparison.Ordinal))
        {
            return false;
        }

        if (component.EndsWith("." + simpleType, StringComparison.Ordinal))
        {
            return false;
        }

        if (component.Contains(simpleType, StringComparison.Ordinal))
        {
            return false;
        }

        if (component == "AppBarButton.Icon" && simpleType is "FontIcon" or "SymbolIcon")
        {
            return false;
        }

        var allowDiagnosticComponents = component is
            "Annotated scrollbar" or
            "Color" or
            "CornerRadius" or
            "ResourceDictionary.ThemeDictionaries" or
            "Setter" or
            "SolidColorBrush" or
            "StaticResource" or
            "Style" or
            "ThemeResource" or
            "Title bar customization" or
            "Window.SystemBackdrop / MicaBackdrop" or
            "XamlControlsResources";
        if (allowDiagnosticComponents && simpleType is "Border" or "ContentPresenter" or "TextBlock")
        {
            return false;
        }

        var allowContainerComponents = component is
            "Border" or
            "Grid" or
            "StackPanel" or
            "ScrollViewer" or
            "CommandBar.Content" or
            "CommandBarFlyout" or
            "Context menu pattern" or
            "MenuFlyout" or
            "Labels and forms" or
            "Shapes" or
            "InkCanvas / InkToolbar";
        if (allowContainerComponents && simpleType is "Border" or "ContentPresenter" or "Ellipse" or "Grid" or "Line" or "Rectangle" or "StackPanel" or "Button" or "CommandBar")
        {
            return false;
        }

        if (simpleType == "FrameworkElement")
        {
            return false;
        }

        return true;
    }

    private static bool IsNativeTargetRequiredScenarioPath(string? scenarioPath)
    {
        return !string.IsNullOrWhiteSpace(scenarioPath) &&
            (scenarioPath.StartsWith("fixtures/ComponentParityLab.WinUI/scenarios/", StringComparison.Ordinal) ||
                scenarioPath.StartsWith("fixtures/PublicAdminWorkbench.WinUI/scenarios/", StringComparison.Ordinal));
    }

    private static bool IsExpectedPublicScenarioPath(string repositoryRoot, string? scenarioPath)
    {
        return !string.IsNullOrWhiteSpace(scenarioPath) &&
            ExpectedPublicScenarioPaths(repositoryRoot)
                .Select(path => RelativePath(repositoryRoot, path))
                .Contains(scenarioPath, StringComparer.Ordinal);
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

    private sealed record ExpectedNativeReferenceTarget(
        string Component,
        string Target);
}
