#if !NATIVE_WINDOWS_UIA
using WinUI3.MacRunner.Automation;

namespace WindowsUiAutomationProbe;

internal static class NativeAutomationRunner
{
    public static async Task<IReadOnlyList<NativeWindowsAutomationActionResult>> RunAsync(
        NativeWindowsAutomationPlan plan,
        NativeWindowsAutomationProbeOptions options)
    {
        var results = plan.Actions
            .Select(action => NativeWindowsAutomationActionResult.Skipped(
                action,
                "Native Windows UIA/FlaUI reference execution requires Windows; no .exe or .msix was launched on this host."))
            .ToArray();
        await Program.WriteJsonAsync(
            plan.WindowsReferencePath,
            WindowsReferenceProvenance.Skipped(plan.ScenarioName, plan.ScenarioPath, "Native Windows reference capture requires Windows."));
        return results;
    }
}
#endif
