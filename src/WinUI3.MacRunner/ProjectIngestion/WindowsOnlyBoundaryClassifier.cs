using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.ProjectIngestion;

/// <summary>
/// Classifies the Windows-only boundaries (WinRT storage, credential lockers,
/// packaged activation, system backdrops, and Windows App SDK deployment) of an
/// inspected WinUI app project. The classifier reads source and XAML text only;
/// it never builds or executes the project, and the boundaries it returns are
/// honest diagnostics that do not block renderable XAML/page output.
/// </summary>
public static class WindowsOnlyBoundaryClassifier
{
    public static IReadOnlyList<WindowsOnlyBoundary> Classify(WinUIProjectModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var xamlFiles = EnumerateXaml(model)
            .Select(file => ReadBoundaryFile(file))
            .OfType<WindowsOnlyBoundaryFile>();
        var sourceFiles = model.SourceFiles
            .Select(file => ReadBoundaryFile(file))
            .OfType<WindowsOnlyBoundaryFile>();

        return WindowsOnlyBoundaryScanner.Scan(
            sourceFiles,
            xamlFiles,
            model.PackageReferences.Select(reference => reference.Include),
            model.WindowsPackageType);
    }

    private static IEnumerable<WinUIProjectFile> EnumerateXaml(WinUIProjectModel model)
    {
        if (model.ApplicationXaml is not null)
        {
            yield return model.ApplicationXaml;
        }

        foreach (var file in model.PageXamlFiles)
        {
            yield return file;
        }

        foreach (var file in model.ResourceDictionaryXamlFiles)
        {
            yield return file;
        }
    }

    private static WindowsOnlyBoundaryFile? ReadBoundaryFile(WinUIProjectFile file)
    {
        try
        {
            return new WindowsOnlyBoundaryFile(file.RelativePath, File.ReadAllText(file.FullPath));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
