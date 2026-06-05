using SkiaSharp;
using WinUI3.MacRuntime;

namespace WinUI3.MacRenderer.Skia;

/// <summary>
/// Resolves the typefaces the skia-v2 renderer uses, keeping normal text font
/// resolution separate from symbol/icon font resolution.
/// </summary>
/// <remarks>
/// <para>
/// WinUI normally draws <c>FontIcon</c>/<c>SymbolIcon</c> glyphs from a dedicated
/// icon font (<c>Segoe Fluent Icons</c> on Windows 11, falling back to
/// <c>Segoe MDL2 Assets</c>), while body text uses <c>Segoe UI</c>. The renderer
/// must not assume either family is installed: these are proprietary Windows
/// fonts that are not bundled with this repository.
/// </para>
/// <para>
/// Resolution is split into a pure, deterministic <see cref="Plan"/> step (which
/// only needs a family-availability predicate, so it is testable without any
/// fonts installed) and a SkiaSharp-backed <see cref="Resolve"/> step that loads
/// the actual typefaces. When no symbol font is available the symbol role reuses
/// the resolved text typeface, so the rendered output is byte-for-byte identical
/// to the renderer's historical single-typeface behavior; the diagnostics record
/// this honestly as <see cref="TextFontFallbackMode"/>.
/// </para>
/// </remarks>
public static class FontResolver
{
    /// <summary>Ordered preference list for normal text, most preferred first.</summary>
    public static readonly IReadOnlyList<string> TextFontCandidates = new[] { "Segoe UI Variable", "Segoe UI" };

    /// <summary>Ordered preference list for symbol/icon glyphs, most preferred first.</summary>
    public static readonly IReadOnlyList<string> SymbolFontCandidates = new[]
    {
        "Segoe Fluent Icons",
        "Segoe MDL2 Assets",
    };

    /// <summary>Role identifier for normal text.</summary>
    public const string TextRole = "text";

    /// <summary>Role identifier for symbol/icon glyphs.</summary>
    public const string SymbolRole = "symbol";

    /// <summary>A requested family was installed and used.</summary>
    public const string RequestedFamilyMode = "requested-family";

    /// <summary>No requested text family was available; the platform default text font was used.</summary>
    public const string PlatformFallbackMode = "platform-fallback";

    /// <summary>No symbol font was available; icon glyphs were drawn with the text typeface.</summary>
    public const string TextFontFallbackMode = "text-font-fallback";

    /// <summary>Typeface came from Skia's default system font manager.</summary>
    public const string SystemFontManagerSource = "system-font-manager";

    /// <summary>Typeface came from an explicitly supplied repo-external font directory.</summary>
    public const string ExternalFontDirectorySource = "external-font-directory";

    /// <summary>Typeface came from the platform default fallback.</summary>
    public const string PlatformDefaultSource = "platform-default";

    /// <summary>Symbol glyphs reused the resolved text typeface.</summary>
    public const string TextFontFallbackSource = "text-font-fallback";

    /// <summary>Path-separated list of repo-external directories used for opt-in font A/B runs.</summary>
    public const string ExternalFontDirectoriesEnvironmentVariable = "WINUI3_MAC_TEST_FONT_DIRS";

    /// <summary>
    /// Decides which family each role resolves to, given a predicate that reports
    /// whether a font family is installed. Pure and deterministic so it can be
    /// tested without any proprietary fonts present.
    /// </summary>
    public static FontPlan Plan(Func<string, bool> isFamilyAvailable)
    {
        ArgumentNullException.ThrowIfNull(isFamilyAvailable);

        return Plan(candidate => isFamilyAvailable(candidate)
            ? new FontFamilyMatch(candidate, SystemFontManagerSource, null)
            : null);
    }

    /// <summary>
    /// Decides which family each role resolves to, preserving source/path
    /// provenance for explicit external font matches.
    /// </summary>
    public static FontPlan Plan(Func<string, FontFamilyMatch?> findFamily)
    {
        ArgumentNullException.ThrowIfNull(findFamily);

        var textMatch = FirstAvailable(TextFontCandidates, findFamily);
        var symbolMatch = FirstAvailable(SymbolFontCandidates, findFamily);

        var text = new FontRolePlan(
            TextRole,
            TextFontCandidates,
            textMatch?.Family,
            textMatch?.Source,
            textMatch?.Path,
            textMatch is not null ? RequestedFamilyMode : PlatformFallbackMode);

        var symbol = new FontRolePlan(
            SymbolRole,
            SymbolFontCandidates,
            symbolMatch?.Family,
            symbolMatch?.Source,
            symbolMatch?.Path,
            symbolMatch is not null ? RequestedFamilyMode : TextFontFallbackMode);

        return new FontPlan(text, symbol);
    }

    /// <summary>
    /// Resolves the actual typefaces using the supplied font manager (the default
    /// system manager when none is given) and reports the detected families.
    /// </summary>
    public static ResolvedFonts Resolve(SKFontManager? fontManager = null, FontResolverOptions? options = null)
    {
        var manager = fontManager ?? SKFontManager.Default;
        var installed = new HashSet<string>(SafeGetFamilies(manager), StringComparer.OrdinalIgnoreCase);
        var configuredOptions = options ?? FontResolverOptions.FromEnvironment();
        var externalFonts = BuildExternalFontIndex(configuredOptions.ExternalFontDirectories);
        var plan = Plan(candidate =>
        {
            if (externalFonts.TryGetValue(candidate, out var externalMatch))
            {
                return externalMatch;
            }

            return installed.Contains(candidate)
                ? new FontFamilyMatch(candidate, SystemFontManagerSource, null)
                : null;
        });

        var text = LoadTextTypeface(plan.Text);

        SKTypeface symbolTypeface;
        bool ownsSymbol;
        string symbolSource;
        string? symbolPath;
        if (plan.Symbol.MatchedFamily is { } symbolFamily)
        {
            var symbol = LoadMatchedTypeface(plan.Symbol, symbolFamily);
            symbolTypeface = symbol.Typeface;
            symbolSource = symbol.Source;
            symbolPath = symbol.Path;
            ownsSymbol = !ReferenceEquals(symbolTypeface, text.Typeface);
        }
        else
        {
            // Honest fallback: no symbol font available, so icons are drawn with the
            // text typeface. Reusing the same instance keeps pixels identical to the
            // renderer's historical behavior.
            symbolTypeface = text.Typeface;
            symbolSource = TextFontFallbackSource;
            symbolPath = text.Path;
            ownsSymbol = false;
        }

        var diagnostics = new SnapshotFontDiagnostics(
            Text: ToDiagnostic(plan.Text, text.Typeface.FamilyName, text.Source, text.Path),
            Symbol: ToDiagnostic(plan.Symbol, symbolTypeface.FamilyName, symbolSource, symbolPath));

        return new ResolvedFonts(text.Typeface, symbolTypeface, ownsSymbol, diagnostics);
    }

    private static FontRoleDiagnostic ToDiagnostic(FontRolePlan plan, string resolvedFamily, string resolvedSource, string? resolvedPath) =>
        new(
            plan.Role,
            plan.RequestedFamilies,
            plan.MatchedFamily,
            resolvedFamily,
            plan.MatchedFamily is not null,
            plan.FallbackMode,
            resolvedSource,
            resolvedPath);

    private static FontFamilyMatch? FirstAvailable(IReadOnlyList<string> candidates, Func<string, FontFamilyMatch?> findFamily)
    {
        foreach (var candidate in candidates)
        {
            if (findFamily(candidate) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private static LoadedFont LoadTextTypeface(FontRolePlan plan)
    {
        if (plan.MatchedFamily is { } family)
        {
            return LoadMatchedTypeface(plan, family);
        }

        return new LoadedFont(LoadOrDefault(TextFontCandidates[0]), PlatformDefaultSource, null);
    }

    private static LoadedFont LoadMatchedTypeface(FontRolePlan plan, string family)
    {
        if (plan.MatchedPath is { } path && TryLoadTypefaceFromFile(path) is { } fileTypeface)
        {
            return new LoadedFont(fileTypeface, plan.MatchedSource ?? ExternalFontDirectorySource, path);
        }

        return new LoadedFont(LoadOrDefault(family), plan.MatchedSource ?? SystemFontManagerSource, null);
    }

    private static SKTypeface LoadOrDefault(string family)
    {
        // FromFamilyName returns the platform default typeface when the family is
        // unavailable, mirroring the renderer's historical behavior.
        return SKTypeface.FromFamilyName(family) ?? SKTypeface.Default;
    }

    private static SKTypeface? TryLoadTypefaceFromFile(string path)
    {
        try
        {
            return SKTypeface.FromFile(path);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> SafeGetFamilies(SKFontManager manager)
    {
        try
        {
            return manager.GetFontFamilies();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyDictionary<string, FontFamilyMatch> BuildExternalFontIndex(IReadOnlyList<string> directories)
    {
        var result = new Dictionary<string, FontFamilyMatch>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in EnumerateExternalFontFiles(directories))
        {
            using var typeface = TryLoadTypefaceFromFile(file);
            var family = typeface?.FamilyName;
            if (string.IsNullOrWhiteSpace(family) || result.ContainsKey(family))
            {
                continue;
            }

            result[family] = new FontFamilyMatch(family, ExternalFontDirectorySource, Path.GetFullPath(file));
        }

        return result;
    }

    private static IEnumerable<string> EnumerateExternalFontFiles(IReadOnlyList<string> directories)
    {
        foreach (var directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                continue;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                if (string.Equals(extension, ".ttf", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".otf", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".ttc", StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }
    }
}

/// <summary>Source/path provenance for a candidate family match.</summary>
public sealed record FontFamilyMatch(string Family, string Source, string? Path);

/// <summary>Resolution decision for a single font role, before typefaces are loaded.</summary>
public readonly record struct FontRolePlan(
    string Role,
    IReadOnlyList<string> RequestedFamilies,
    string? MatchedFamily,
    string? MatchedSource,
    string? MatchedPath,
    string FallbackMode);

/// <summary>The text and symbol resolution decisions for a snapshot.</summary>
public sealed record FontPlan(FontRolePlan Text, FontRolePlan Symbol);

/// <summary>Opt-in settings for repo-external font discovery.</summary>
public sealed record FontResolverOptions(IReadOnlyList<string> ExternalFontDirectories)
{
    public static FontResolverOptions FromEnvironment()
    {
        var value = Environment.GetEnvironmentVariable(FontResolver.ExternalFontDirectoriesEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return new FontResolverOptions(Array.Empty<string>());
        }

        var directories = value
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static directory => !string.IsNullOrWhiteSpace(directory))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new FontResolverOptions(directories);
    }
}

internal sealed record LoadedFont(SKTypeface Typeface, string Source, string? Path);

/// <summary>
/// Loaded typefaces plus the diagnostics describing how they were resolved.
/// Disposing releases only the typefaces this resolver created.
/// </summary>
public sealed class ResolvedFonts : IDisposable
{
    private readonly bool ownsSymbol;

    public ResolvedFonts(SKTypeface textTypeface, SKTypeface symbolTypeface, bool ownsSymbol, SnapshotFontDiagnostics diagnostics)
    {
        TextTypeface = textTypeface;
        SymbolTypeface = symbolTypeface;
        this.ownsSymbol = ownsSymbol;
        Diagnostics = diagnostics;
    }

    public SKTypeface TextTypeface { get; }

    public SKTypeface SymbolTypeface { get; }

    public SnapshotFontDiagnostics Diagnostics { get; }

    public void Dispose()
    {
        if (ownsSymbol)
        {
            SymbolTypeface.Dispose();
        }

        TextTypeface.Dispose();
    }
}
