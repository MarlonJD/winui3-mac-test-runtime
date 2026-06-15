using WinUI3.MacCompatibility;

namespace WinUI3.MacRuntime;

/// <summary>
/// A single Windows-only boundary that direct project ingestion detected in a
/// source project. Boundaries are honest diagnostics, not render blockers: the
/// macOS source-level host still renders supported XAML/page surfaces while
/// recording that these Windows-only behaviors were skipped or diagnosed.
/// </summary>
public sealed record WindowsOnlyBoundary(
    string Boundary,
    string Api,
    string Status,
    string Symbol,
    string? FilePath,
    int? Line,
    string Reason,
    bool BlocksRender);

/// <summary>Source or XAML file text presented to the boundary scanner.</summary>
public sealed record WindowsOnlyBoundaryFile(string RelativePath, string Text);

/// <summary>
/// Classifies Windows-only boundaries (WinRT storage, credential lockers,
/// packaged activation, system backdrops, and Windows App SDK deployment) from a
/// source project. The scanner never executes the project; it inspects source,
/// XAML, package references, and project properties only.
/// </summary>
public static class WindowsOnlyBoundaryScanner
{
    private const string StorageBoundary = "windows-storage";
    private const string CredentialsBoundary = "windows-credentials";
    private const string PackagedActivationBoundary = "packaged-activation";
    private const string SystemBackdropBoundary = "system-backdrop";
    private const string DeploymentBoundary = "windows-app-sdk-deployment";

    private static readonly SourceRule[] SourceRules =
    {
        new(
            StorageBoundary,
            "Windows.Storage.ApplicationData",
            "ApplicationData",
            CompatibilityStatuses.WindowsOnly,
            "Windows.Storage.ApplicationData is a WinRT per-user/package settings store that runs only on Windows; the macOS source-level host does not provide WinRT application data containers."),
        new(
            CredentialsBoundary,
            "Windows.Security.Credentials.PasswordVault",
            "PasswordVault",
            CompatibilityStatuses.WindowsOnly,
            "Windows.Security.Credentials.PasswordVault is the Windows credential locker and is not executed by the macOS source-level host."),
        new(
            PackagedActivationBoundary,
            "Microsoft.Windows.AppLifecycle.AppInstance",
            "AppInstance",
            CompatibilityStatuses.WindowsOnly,
            "Packaged app activation through Microsoft.Windows.AppLifecycle.AppInstance is validated on real Windows, not executed on macOS."),
        new(
            SystemBackdropBoundary,
            "Microsoft.UI.Xaml.Media.MicaBackdrop",
            "MicaBackdrop",
            CompatibilityStatuses.Planned,
            "MicaBackdrop is a Windows system backdrop; the macOS host records a strict diagnostic and renders no Mica material."),
        new(
            SystemBackdropBoundary,
            "Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop",
            "DesktopAcrylicBackdrop",
            CompatibilityStatuses.Planned,
            "DesktopAcrylicBackdrop is a Windows system backdrop; the macOS host records a strict diagnostic and renders no Acrylic material."),
        new(
            SystemBackdropBoundary,
            "Microsoft.UI.Xaml.Media.SystemBackdrop",
            "SystemBackdrop",
            CompatibilityStatuses.Planned,
            "Window.SystemBackdrop targets Windows materials; the macOS host records a diagnostic instead of rendering a system backdrop."),
    };

    public static IReadOnlyList<WindowsOnlyBoundary> Scan(
        IEnumerable<WindowsOnlyBoundaryFile> sourceFiles,
        IEnumerable<WindowsOnlyBoundaryFile> xamlFiles,
        IEnumerable<string> packageReferences,
        string? windowsPackageType,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        var boundaries = new List<WindowsOnlyBoundary>();

        foreach (var file in sourceFiles.Concat(xamlFiles))
        {
            if (file is null || string.IsNullOrEmpty(file.Text))
            {
                continue;
            }

            foreach (var rule in SourceRules)
            {
                var line = FindFirstLine(file.Text, rule.Symbol);
                if (line is null)
                {
                    continue;
                }

                boundaries.Add(new WindowsOnlyBoundary(
                    rule.Boundary,
                    rule.Api,
                    ResolveStatus(rule.Api, rule.DefaultStatus),
                    rule.Symbol,
                    file.RelativePath,
                    line,
                    rule.Reason,
                    BlocksRender: false));
            }
        }

        foreach (var package in packageReferences ?? Enumerable.Empty<string>())
        {
            if (string.Equals(package, "Microsoft.WindowsAppSDK", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(package, "Microsoft.Windows.SDK.BuildTools", StringComparison.OrdinalIgnoreCase))
            {
                boundaries.Add(new WindowsOnlyBoundary(
                    DeploymentBoundary,
                    package,
                    ResolveStatus(package, CompatibilityStatuses.WindowsOnly),
                    package,
                    FilePath: null,
                    Line: null,
                    Reason: $"{package} provides Windows App SDK deployment/build targets; the macOS source-level host substitutes managed compatibility facades and does not deploy the Windows App SDK.",
                    BlocksRender: false));
            }
        }

        if (IsPackaged(windowsPackageType))
        {
            boundaries.Add(new WindowsOnlyBoundary(
                PackagedActivationBoundary,
                "Windows.ApplicationModel.Package",
                CompatibilityStatuses.WindowsOnly,
                $"WindowsPackageType={windowsPackageType}",
                FilePath: null,
                Line: null,
                Reason: $"WindowsPackageType '{windowsPackageType}' implies packaged Windows identity and activation, which is a Windows-only behavior the macOS host does not execute.",
                BlocksRender: false));
        }

        if (properties is not null &&
            properties.TryGetValue("WindowsAppSDKSelfContained", out var selfContained) &&
            string.Equals(selfContained, "true", StringComparison.OrdinalIgnoreCase))
        {
            boundaries.Add(new WindowsOnlyBoundary(
                DeploymentBoundary,
                "WindowsAppSDKSelfContained",
                ResolveStatus("WindowsAppSDKSelfContained", CompatibilityStatuses.Planned),
                "WindowsAppSDKSelfContained",
                FilePath: null,
                Line: null,
                Reason: "Self-contained Windows App SDK deployment is a Windows packaging feature and is not part of the macOS source-level host.",
                BlocksRender: false));
        }

        return boundaries
            .GroupBy(boundary => (boundary.Boundary, boundary.Api, boundary.FilePath), TupleComparer)
            .Select(group => group.OrderBy(boundary => boundary.Line ?? int.MaxValue).First())
            .OrderBy(boundary => boundary.Boundary, StringComparer.Ordinal)
            .ThenBy(boundary => boundary.Api, StringComparer.Ordinal)
            .ThenBy(boundary => boundary.FilePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ResolveStatus(string api, string defaultStatus)
    {
        return CompatibilityCatalog.Current.FindByApi(api)?.Status ?? defaultStatus;
    }

    private static bool IsPackaged(string? windowsPackageType)
    {
        return !string.IsNullOrWhiteSpace(windowsPackageType) &&
            !string.Equals(windowsPackageType, "None", StringComparison.OrdinalIgnoreCase);
    }

    private static int? FindFirstLine(string text, string symbol)
    {
        var index = text.IndexOf(symbol, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var line = 1;
        for (var position = 0; position < index; position++)
        {
            if (text[position] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private static readonly IEqualityComparer<(string, string, string?)> TupleComparer =
        new BoundaryKeyComparer();

    private sealed record SourceRule(
        string Boundary,
        string Api,
        string Symbol,
        string DefaultStatus,
        string Reason);

    private sealed class BoundaryKeyComparer : IEqualityComparer<(string Boundary, string Api, string? FilePath)>
    {
        public bool Equals((string Boundary, string Api, string? FilePath) x, (string Boundary, string Api, string? FilePath) y)
        {
            return string.Equals(x.Boundary, y.Boundary, StringComparison.Ordinal) &&
                string.Equals(x.Api, y.Api, StringComparison.Ordinal) &&
                string.Equals(x.FilePath, y.FilePath, StringComparison.Ordinal);
        }

        public int GetHashCode((string Boundary, string Api, string? FilePath) obj)
        {
            return HashCode.Combine(obj.Boundary, obj.Api, obj.FilePath);
        }
    }
}
