using System.Diagnostics;
using System.Text.Json;
using WinUI3.MacRunner.Automation;
using WinUI3.MacRuntime;

namespace WindowsUiAutomationProbe;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = ProbeOptions.Parse(args);
            var scenario = await VisualScenario.LoadAsync(options.ScenarioPath);
            var probeOptions = new NativeWindowsAutomationProbeOptions(
                options.OutputDirectory,
                options.WindowTitle,
                options.AppCommand,
                options.AttachProcessId,
                options.CaptureToolPath,
                options.Timeout);
            var plan = NativeWindowsAutomationPlan.Create(scenario, options.ScenarioPath, probeOptions);
            Directory.CreateDirectory(plan.OutputDirectory);

            var results = await NativeAutomationRunner.RunAsync(plan, probeOptions);
            var captureSucceeded = await WriteWindowsReferenceAsync(plan);
            WriteNativeAutomationReport(plan, results);
            Console.WriteLine($"native-automation.json: {plan.NativeAutomationPath}");
            Console.WriteLine($"windows-reference.json: {plan.WindowsReferencePath}");
            if (plan.WindowsReferencePngPath is not null)
            {
                Console.WriteLine($"windows-reference.png: {plan.WindowsReferencePngPath}");
            }

            return results.Any(result => result.Status == "failed") || !captureSucceeded ? 1 : 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintUsage();
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    internal static void WriteNativeAutomationReport(
        NativeWindowsAutomationPlan plan,
        IReadOnlyList<NativeWindowsAutomationActionResult> results)
    {
        var report = NativeWindowsAutomationReport.FromResults(
            plan.ScenarioName,
            plan.ScenarioPath,
            plan.OutputDirectory,
            results,
            plan.WindowsReferencePath,
            plan.WindowsReferencePngPath);
        File.WriteAllText(
            plan.NativeAutomationPath,
            JsonSerializer.Serialize(report, NativeWindowsAutomationJson.Options));
    }

    internal static async Task WriteJsonAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, NativeWindowsAutomationJson.Options));
    }

    private static async Task<bool> WriteWindowsReferenceAsync(NativeWindowsAutomationPlan plan)
    {
        if (plan.Capture is null)
        {
            await WriteJsonAsync(
                plan.WindowsReferencePath,
                WindowsReferenceProvenance.Skipped(plan.ScenarioName, plan.ScenarioPath, "Scenario did not request visual capture."));
            return true;
        }

        if (!OperatingSystem.IsWindows())
        {
            await WriteJsonAsync(
                plan.WindowsReferencePath,
                WindowsReferenceProvenance.Skipped(plan.ScenarioName, plan.ScenarioPath, "WindowsWindowCapture requires Windows; screenshot capture was skipped."));
            return true;
        }

        var startInfo = CreateCaptureStartInfo(plan.Capture);
        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Could not start WindowsWindowCapture.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (stdout.Length > 0)
        {
            Console.Write(stdout);
        }

        if (stderr.Length > 0)
        {
            Console.Error.Write(stderr);
        }

        return process.ExitCode == 0;
    }

    private static ProcessStartInfo CreateCaptureStartInfo(NativeWindowsAutomationCapturePlan capture)
    {
        var toolPath = capture.ToolPath ?? DefaultCaptureToolPath();
        var startInfo = toolPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? new ProcessStartInfo("dotnet")
            : new ProcessStartInfo(toolPath);
        if (toolPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(toolPath);
            startInfo.ArgumentList.Add("--");
        }

        foreach (var argument in capture.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        return startInfo;
    }

    private static string DefaultCaptureToolPath()
    {
        var probeDirectory = AppContext.BaseDirectory;
        var repositoryRoot = Path.GetFullPath(Path.Combine(probeDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repositoryRoot, "tools", "WindowsWindowCapture", "WindowsWindowCapture.csproj");
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: WindowsUiAutomationProbe --scenario <json> --output <dir> --title <window title> [--attach-pid <pid>] [--capture-tool <path>] [--timeout-seconds 30] -- <app command> [args...]");
    }
}

internal sealed record ProbeOptions(
    string ScenarioPath,
    string OutputDirectory,
    string WindowTitle,
    int? AttachProcessId,
    string? CaptureToolPath,
    TimeSpan Timeout,
    IReadOnlyList<string> AppCommand)
{
    public static ProbeOptions Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help", StringComparer.Ordinal) || args.Contains("-h", StringComparer.Ordinal))
        {
            throw new ArgumentException("Missing required options.");
        }

        var separator = Array.IndexOf(args, "--");
        var optionEnd = separator >= 0 ? separator : args.Length;
        string? scenarioPath = null;
        string? outputDirectory = null;
        string? windowTitle = null;
        int? attachProcessId = null;
        string? captureToolPath = null;
        var timeout = TimeSpan.FromSeconds(30);

        for (var index = 0; index < optionEnd; index++)
        {
            switch (args[index])
            {
                case "--scenario":
                    scenarioPath = ReadValue(args, ++index, "--scenario");
                    break;
                case "--output":
                    outputDirectory = ReadValue(args, ++index, "--output");
                    break;
                case "--title":
                    windowTitle = ReadValue(args, ++index, "--title");
                    break;
                case "--attach-pid":
                    attachProcessId = int.Parse(ReadValue(args, ++index, "--attach-pid"), System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--capture-tool":
                    captureToolPath = ReadValue(args, ++index, "--capture-tool");
                    break;
                case "--timeout-seconds":
                    timeout = TimeSpan.FromSeconds(double.Parse(ReadValue(args, ++index, "--timeout-seconds"), System.Globalization.CultureInfo.InvariantCulture));
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[index]}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            throw new ArgumentException("Missing required option: --scenario <json>");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Missing required option: --output <dir>");
        }

        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            throw new ArgumentException("Missing required option: --title <window title>");
        }

        var command = separator >= 0 ? args[(separator + 1)..] : Array.Empty<string>();
        if (attachProcessId is null && command.Length == 0)
        {
            throw new ArgumentException("Missing app command after '--'.");
        }

        return new ProbeOptions(
            Path.GetFullPath(scenarioPath),
            Path.GetFullPath(outputDirectory),
            windowTitle,
            attachProcessId,
            captureToolPath,
            timeout,
            command);
    }

    private static string ReadValue(string[] args, int index, string option)
    {
        if (index >= args.Length || args[index] == "--")
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        return args[index];
    }
}
