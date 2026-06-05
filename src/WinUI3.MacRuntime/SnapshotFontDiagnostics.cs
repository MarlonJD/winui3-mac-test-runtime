namespace WinUI3.MacRuntime;

/// <summary>
/// Renderer font-resolution diagnostics for a single snapshot.
/// </summary>
/// <remarks>
/// These values describe which fonts a snapshot renderer actually used for normal
/// text versus symbol/icon glyphs. They exist so visual evidence can be read
/// honestly without bundling proprietary Windows fonts: a reviewer can tell
/// whether an icon row was painted with a real symbol font or with a text-font
/// fallback. The diagnostics never claim a font is present unless the renderer
/// actually resolved it.
/// </remarks>
public sealed record SnapshotFontDiagnostics(
    FontRoleDiagnostic Text,
    FontRoleDiagnostic Symbol);

/// <summary>
/// Resolution outcome for one font role (normal text or symbol/icon glyphs).
/// </summary>
/// <param name="Role">The font role this diagnostic describes: <c>"text"</c> or <c>"symbol"</c>.</param>
/// <param name="RequestedFamilies">
/// The ordered preference list the resolver consulted, most preferred first.
/// </param>
/// <param name="MatchedFamily">
/// The first requested family that was actually installed, or <c>null</c> when
/// none of the requested families were available.
/// </param>
/// <param name="ResolvedFamily">
/// The family name of the typeface the renderer actually drew with. When no
/// requested family is available this is the platform fallback family (for text)
/// or the text fallback family (for symbol), not a requested name.
/// </param>
/// <param name="RequestedFamilyAvailable">
/// <c>true</c> when a requested family was installed and used; <c>false</c> when a
/// fallback was used instead.
/// </param>
/// <param name="FallbackMode">
/// How the typeface was chosen. For text: <c>"requested-family"</c> or
/// <c>"platform-fallback"</c>. For symbol: <c>"requested-family"</c> or
/// <c>"text-font-fallback"</c> (icons drawn with the text typeface because no
/// symbol font was available).
/// </param>
/// <param name="ResolvedSource">
/// Provenance for the typeface the renderer actually drew with, such as the
/// system font manager, an explicitly supplied external font directory, the
/// platform default fallback, or the text-font fallback used by symbol glyphs.
/// </param>
/// <param name="ResolvedPath">
/// Absolute path to the resolved font file when the typeface came from an
/// opt-in external font directory. This is diagnostic provenance only; font
/// files are never embedded in visual evidence.
/// </param>
public sealed record FontRoleDiagnostic(
    string Role,
    IReadOnlyList<string> RequestedFamilies,
    string? MatchedFamily,
    string ResolvedFamily,
    bool RequestedFamilyAvailable,
    string FallbackMode,
    string ResolvedSource = "unknown",
    string? ResolvedPath = null);
