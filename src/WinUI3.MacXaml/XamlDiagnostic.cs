namespace WinUI3.MacXaml;

public sealed record XamlDiagnostic(
    string Code,
    string Message,
    string Severity,
    string? FilePath,
    int? Line,
    int? Column);

public sealed record XamlCompilationResult(
    bool Succeeded,
    string GeneratedSource,
    IReadOnlyList<XamlDiagnostic> Diagnostics);
