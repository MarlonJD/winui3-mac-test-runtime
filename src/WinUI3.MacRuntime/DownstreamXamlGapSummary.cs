using System.Text.Json;
using System.Text.RegularExpressions;

namespace WinUI3.MacRuntime;

public sealed record DownstreamXamlGapSummary(
    string SchemaVersion,
    string BaselineDate,
    string Scope,
    int TotalDiagnostics,
    IReadOnlyList<DownstreamXamlDiagnosticCodeCount> DiagnosticCodes,
    IReadOnlyList<DownstreamXamlFileCategoryCount> FileCategories,
    IReadOnlyList<DownstreamXamlSurfaceFamily> SurfaceFamilies)
{
    private static readonly Regex QuotedSurfacePattern = new("'([^']+)'", RegexOptions.Compiled);

    public static DownstreamXamlGapSummary Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<DownstreamXamlGapSummary>(stream, JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Unable to read downstream XAML gap summary from {path}.");
    }

    public static DownstreamXamlGapSummary FromDiagnostics(
        IEnumerable<DownstreamXamlDiagnosticRecord> diagnostics,
        Func<string?, string> fileCategoryClassifier)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(fileCategoryClassifier);

        var records = diagnostics.ToArray();
        var diagnosticCodes = records
            .GroupBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new DownstreamXamlDiagnosticCodeCount(group.Key, group.Count()))
            .ToArray();
        var fileCategories = records
            .GroupBy(diagnostic => fileCategoryClassifier(diagnostic.FilePath), StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new DownstreamXamlFileCategoryCount(group.Key, group.Count()))
            .ToArray();
        var surfaceFamilies = records
            .GroupBy(diagnostic => ExtractSurface(diagnostic.Message), StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new DownstreamXamlSurfaceFamily(
                group.Key,
                group.Count(),
                TreatmentFor(group.Key),
                group.Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()))
            .ToArray();

        return new DownstreamXamlGapSummary(
            "0.1",
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            "sanitized downstream production XAML diagnostics",
            records.Length,
            diagnosticCodes,
            fileCategories,
            surfaceFamilies);
    }

    public static string DefaultFileCategoryClassifier(string? filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath ?? string.Empty);
        return fileName switch
        {
            var name when name.Contains("Admin", StringComparison.OrdinalIgnoreCase) => "admin-workbench",
            var name when name.Contains("Home", StringComparison.OrdinalIgnoreCase) => "home-read-surface",
            var name when name.Contains("Messages", StringComparison.OrdinalIgnoreCase) => "messages",
            var name when name.Contains("Channels", StringComparison.OrdinalIgnoreCase) => "channels",
            var name when name.Contains("Notifications", StringComparison.OrdinalIgnoreCase) => "notifications",
            var name when name.Contains("Settings", StringComparison.OrdinalIgnoreCase) => "settings",
            var name when name.Contains("Events", StringComparison.OrdinalIgnoreCase) => "events",
            var name when name.Contains("Login", StringComparison.OrdinalIgnoreCase) => "login",
            var name when name.Contains("Theme", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Component", StringComparison.OrdinalIgnoreCase) => "theme-dictionaries",
            _ => "other"
        };
    }

    private static string ExtractSurface(string message)
    {
        var match = QuotedSurfacePattern.Match(message);
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    private static string TreatmentFor(string surface)
    {
        if (surface.Contains("Grid.", StringComparison.Ordinal) ||
            surface.Contains("Width", StringComparison.Ordinal) ||
            surface.Contains("Height", StringComparison.Ordinal) ||
            surface.Contains("Padding", StringComparison.Ordinal) ||
            surface.Contains("Border", StringComparison.Ordinal) ||
            surface.Contains("ScrollViewer.", StringComparison.Ordinal))
        {
            return "layout gap";
        }

        if (surface.Contains("Text", StringComparison.Ordinal) || surface == "PasswordBox")
        {
            return "text/form gap";
        }

        if (surface.Contains("ListView", StringComparison.Ordinal) ||
            surface.Contains("ItemsControl.ItemTemplate", StringComparison.Ordinal))
        {
            return "list/template gap";
        }

        if (surface.Contains("CommandBar", StringComparison.Ordinal))
        {
            return "command gap";
        }

        if (surface.Contains("InfoBar", StringComparison.Ordinal) ||
            surface.Contains("ProgressRing", StringComparison.Ordinal) ||
            surface.Contains("SizeChanged", StringComparison.Ordinal))
        {
            return "status/lifecycle gap";
        }

        if (surface.Contains("ResourceDictionary", StringComparison.Ordinal) ||
            surface.Contains("resource dictionary", StringComparison.OrdinalIgnoreCase))
        {
            return "resource dictionary input gap";
        }

        return "unsupported surface gap";
    }
}

public sealed record DownstreamXamlDiagnosticRecord(
    string Code,
    string Message,
    string Severity,
    string? FilePath,
    int? Line,
    int? Column);

public sealed record DownstreamXamlDiagnosticCodeCount(string Code, int Count);

public sealed record DownstreamXamlFileCategoryCount(string Category, int Count);

public sealed record DownstreamXamlSurfaceFamily(
    string Surface,
    int Count,
    string CurrentTreatment,
    IReadOnlyList<string> DiagnosticCodes);
