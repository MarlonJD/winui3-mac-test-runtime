using WinUI3.MacRenderer.Skia;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class FontResolverTests
{
    private static Func<string, bool> Installed(params string[] families)
    {
        var set = new HashSet<string>(families, StringComparer.OrdinalIgnoreCase);
        return set.Contains;
    }

    [TestMethod]
    public void CandidateListsAreOrderedAndDoNotMixTextWithSymbol()
    {
        CollectionAssert.AreEqual(new[] { "Segoe UI Variable", "Segoe UI" }, FontResolver.TextFontCandidates.ToArray());
        CollectionAssert.AreEqual(
            new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" },
            FontResolver.SymbolFontCandidates.ToArray());
    }

    [TestMethod]
    public void TextPrefersSegoeUiVariableWhenAvailable()
    {
        var plan = FontResolver.Plan(Installed("Segoe UI Variable", "Segoe UI"));

        Assert.AreEqual(FontResolver.TextRole, plan.Text.Role);
        Assert.AreEqual("Segoe UI Variable", plan.Text.MatchedFamily);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Text.FallbackMode);
    }

    [TestMethod]
    public void TextUsesSegoeUiWhenAvailable()
    {
        var plan = FontResolver.Plan(Installed("Segoe UI"));

        Assert.AreEqual(FontResolver.TextRole, plan.Text.Role);
        Assert.AreEqual("Segoe UI", plan.Text.MatchedFamily);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Text.FallbackMode);
    }

    [TestMethod]
    public void TextFallsBackToPlatformWhenSegoeUiMissing()
    {
        var plan = FontResolver.Plan(Installed("Helvetica", "Arial"));

        Assert.IsNull(plan.Text.MatchedFamily);
        Assert.AreEqual(FontResolver.PlatformFallbackMode, plan.Text.FallbackMode);
    }

    [TestMethod]
    public void SymbolPrefersSegoeFluentIconsOverMdl2()
    {
        var plan = FontResolver.Plan(Installed("Segoe Fluent Icons", "Segoe MDL2 Assets"));

        Assert.AreEqual(FontResolver.SymbolRole, plan.Symbol.Role);
        Assert.AreEqual("Segoe Fluent Icons", plan.Symbol.MatchedFamily);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Symbol.FallbackMode);
    }

    [TestMethod]
    public void SymbolFallsBackToMdl2WhenFluentMissing()
    {
        var plan = FontResolver.Plan(Installed("Segoe MDL2 Assets"));

        Assert.AreEqual("Segoe MDL2 Assets", plan.Symbol.MatchedFamily);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Symbol.FallbackMode);
    }

    [TestMethod]
    public void SymbolFallsBackToTextFontWhenNoSymbolFontAvailable()
    {
        var plan = FontResolver.Plan(Installed("Segoe UI"));

        Assert.IsNull(plan.Symbol.MatchedFamily);
        Assert.AreEqual(FontResolver.TextFontFallbackMode, plan.Symbol.FallbackMode);
    }

    [TestMethod]
    public void TextAndSymbolResolveIndependently()
    {
        // Text font present, but no symbol font: text is satisfied while symbol
        // must drop to the text-font fallback. The two roles never share a list.
        var plan = FontResolver.Plan(Installed("Segoe UI"));

        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Text.FallbackMode);
        Assert.AreEqual(FontResolver.TextFontFallbackMode, plan.Symbol.FallbackMode);
    }

    [TestMethod]
    public void CaseInsensitiveFamilyMatching()
    {
        var plan = FontResolver.Plan(Installed("segoe ui", "segoe fluent icons"));

        Assert.AreEqual("Segoe UI", plan.Text.MatchedFamily);
        Assert.AreEqual("Segoe Fluent Icons", plan.Symbol.MatchedFamily);
    }

    [TestMethod]
    public void ExternalFontDirectoryMatchCarriesPathProvenance()
    {
        var plan = FontResolver.Plan(candidate =>
            string.Equals(candidate, "Segoe UI", StringComparison.OrdinalIgnoreCase)
                ? new FontFamilyMatch(candidate, FontResolver.ExternalFontDirectorySource, "/repo-outside/SegoeUI.ttf")
                : null);

        Assert.AreEqual("Segoe UI", plan.Text.MatchedFamily);
        Assert.AreEqual(FontResolver.ExternalFontDirectorySource, plan.Text.MatchedSource);
        Assert.AreEqual("/repo-outside/SegoeUI.ttf", plan.Text.MatchedPath);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Text.FallbackMode);
    }

    [TestMethod]
    public void PlanRejectsNullPredicate()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => FontResolver.Plan((Func<string, bool>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => FontResolver.Plan((Func<string, FontFamilyMatch?>)null!));
    }

    [TestMethod]
    public void ResolveProducesConsistentDiagnosticsInCurrentEnvironment()
    {
        // Runs against the real default font manager. It must not require any
        // proprietary font: it only asserts the diagnostics are self-consistent
        // for whatever fonts happen to be installed on the host.
        using var fonts = FontResolver.Resolve();
        var diagnostics = fonts.Diagnostics;

        Assert.AreEqual(FontResolver.TextRole, diagnostics.Text.Role);
        Assert.AreEqual(FontResolver.SymbolRole, diagnostics.Symbol.Role);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostics.Text.ResolvedFamily));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostics.Symbol.ResolvedFamily));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostics.Text.ResolvedSource));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostics.Symbol.ResolvedSource));

        // RequestedFamilyAvailable must agree with whether a family matched.
        Assert.AreEqual(diagnostics.Text.MatchedFamily is not null, diagnostics.Text.RequestedFamilyAvailable);
        Assert.AreEqual(diagnostics.Symbol.MatchedFamily is not null, diagnostics.Symbol.RequestedFamilyAvailable);

        // When no symbol font is available the symbol role must fall back to the
        // text typeface, both in the reported family and the loaded typeface.
        if (!diagnostics.Symbol.RequestedFamilyAvailable)
        {
            Assert.AreEqual(FontResolver.TextFontFallbackMode, diagnostics.Symbol.FallbackMode);
            Assert.AreEqual(FontResolver.TextFontFallbackSource, diagnostics.Symbol.ResolvedSource);
            Assert.AreEqual(diagnostics.Text.ResolvedFamily, diagnostics.Symbol.ResolvedFamily);
            Assert.AreSame(fonts.TextTypeface, fonts.SymbolTypeface);
        }
    }
}
