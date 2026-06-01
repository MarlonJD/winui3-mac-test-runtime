using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using WinUI3.MacCompat.Diagnostics;

namespace WinUI3.MacRuntime;

public static class ArtifactSchemas
{
    public const string Accessibility = "0.2";
    public const string BindingFailures = "0.1";
    public const string DoctorReport = "0.1";
    public const string InteractionReport = "0.1";
    public const string InteractionScript = "0.1";
    public const string PixelDiff = "0.1";
    public const string RunReport = "0.1";
    public const string ResourceFailures = "0.1";
    public const string Scenario = "0.1";
    public const string SkiaV2Snapshot = "0.2";
    public const string Snapshot = "0.1";
    public const string UiTree = "0.1";
    public const string UnsupportedApis = "0.1";
    public const string VisualRun = "0.1";
    public const string VisualUiTree = "0.2";
}

public static class DiagnosticRuleIds
{
    public const string BindingFailure = "WINUI3MAC001";
    public const string ResourceFailure = "WINUI3MAC002";
    public const string UnsupportedApi = "WINUI3MAC003";
}

public sealed record BindingFailureDocument(
    string SchemaVersion,
    IReadOnlyList<BindingFailure> Failures);

public sealed record ResourceFailureDocument(
    string SchemaVersion,
    IReadOnlyList<ResourceLookupFailure> Failures);

public sealed record UnsupportedApiDocument(
    string SchemaVersion,
    IReadOnlyList<UnsupportedApiEntry> Apis);
