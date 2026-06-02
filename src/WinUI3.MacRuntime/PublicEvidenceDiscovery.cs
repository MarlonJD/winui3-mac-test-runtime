namespace WinUI3.MacRuntime;

public static class PublicEvidenceDiscovery
{
    public static IReadOnlyList<string> FindCanonicalEvidenceFiles(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var evidenceRoot = Path.Combine(repositoryRoot, "docs", "visual-parity", "examples");
        if (!Directory.Exists(evidenceRoot))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(evidenceRoot, "component-evidence.json", SearchOption.AllDirectories)
            .Where(path => IsCanonicalEvidencePath(evidenceRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsCanonicalEvidencePath(string evidenceRoot, string path)
    {
        var relativePath = Path.GetRelativePath(evidenceRoot, path);
        var directory = Path.GetDirectoryName(relativePath);
        return !string.IsNullOrWhiteSpace(directory) &&
            !directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Contains("visual", StringComparer.Ordinal);
    }
}
