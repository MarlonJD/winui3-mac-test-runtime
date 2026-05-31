namespace WinUI3.MacXaml;

public sealed record XamlDiagnostic(string Code, string Message, string Severity);

public sealed record MacXamlCompilationResult(
    bool Succeeded,
    IReadOnlyList<XamlDiagnostic> Diagnostics);

public sealed class MacXamlCompiler
{
    public MacXamlCompilationResult CompileCSharpOnlyFixture()
    {
        return new MacXamlCompilationResult(Succeeded: true, Diagnostics: Array.Empty<XamlDiagnostic>());
    }
}
