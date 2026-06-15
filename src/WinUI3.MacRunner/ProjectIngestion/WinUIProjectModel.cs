namespace WinUI3.MacRunner.ProjectIngestion;

public sealed record WinUIProjectModel(
    string ProjectPath,
    string RootDirectory,
    string? TargetFramework,
    bool UseWinUI,
    string? WindowsPackageType,
    IReadOnlyList<WinUIPackageReference> PackageReferences,
    IReadOnlyList<WinUIProjectReference> ProjectReferences,
    WinUIProjectFile? ApplicationXaml,
    IReadOnlyList<WinUIProjectFile> PageXamlFiles,
    IReadOnlyList<WinUIProjectFile> ResourceDictionaryXamlFiles,
    IReadOnlyList<WinUIProjectFile> ContentAssets,
    IReadOnlyList<WinUIProjectFile> SourceFiles)
{
    public bool IsWindowsTargetedWinUI =>
        TargetFramework?.Contains("-windows", StringComparison.OrdinalIgnoreCase) == true && UseWinUI;
}

public sealed record WinUIPackageReference(
    string Include,
    string? Version);

public sealed record WinUIProjectReference(
    string Include,
    string FullPath);

public sealed record WinUIProjectFile(
    string RelativePath,
    string FullPath,
    string ItemType);

public sealed record GeneratedHostOptions(
    string? RootDirectory = null,
    string Configuration = "Debug");

public sealed record GeneratedHostResult(
    string RootDirectory,
    string ProjectPath,
    IReadOnlyList<WinUIProjectFile> LinkedXamlFiles,
    IReadOnlyList<WinUIProjectFile> LinkedContentAssets);
