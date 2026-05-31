using System.Text.Json;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;
using WinUI3.MacXaml;

return await Cli.RunAsync(args);

internal static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        var invokedName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        if (invokedName.Contains("doctor", StringComparison.OrdinalIgnoreCase))
        {
            return RunDoctor(args);
        }

        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        return args[0] switch
        {
            "doctor" => RunDoctor(args[1..]),
            "run" => await RunProjectAsync(args[1..]),
            "xaml" => RunXaml(args[1..]),
            _ => UnknownCommand(args[0])
        };
    }

    private static int RunDoctor(string[] args)
    {
        var json = args.Contains("--json", StringComparer.Ordinal);
        var report = MacDoctor.Check();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, JsonDefaults.Options));
            return 0;
        }

        Console.WriteLine("winui3-mac-doctor");
        Console.WriteLine($"Status: {report.Status}");
        Console.WriteLine($"Host: {report.Host}");
        Console.WriteLine($"Primary path requires Wine: {report.PrimaryPathRequiresWine}");
        Console.WriteLine($"Wine: {report.Wine.Status}, required={report.Wine.Required}, found={report.Wine.Found}");
        Console.WriteLine($".NET: {report.DotNetVersion}");
        Console.WriteLine($"OS: {report.OsDescription} ({report.Architecture})");
        return 0;
    }

    private static async Task<int> RunProjectAsync(string[] args)
    {
        var projectPath = ReadOption(args, "--project");
        if (projectPath is null)
        {
            Console.Error.WriteLine("Missing required option: --project <path>");
            return 2;
        }

        var configuration = ReadOption(args, "--configuration") ?? "Debug";
        var outputDirectory = ReadOption(args, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "artifacts", "winui3-mac");
        var scriptPath = ReadOption(args, "--script");
        var rendererName = ReadOption(args, "--renderer") ?? "svg";

        try
        {
            var runner = new MacProjectRunner(CreateSnapshotRenderer(rendererName));
            var result = await runner.RunProjectAsync(projectPath, outputDirectory, configuration, scriptPath);
            Console.WriteLine($"Status: {result.Run.Status}");
            Console.WriteLine($"run.json: {result.RunJsonPath}");
            Console.WriteLine($"tree.json: {result.TreeJsonPath}");
            Console.WriteLine($"accessibility.json: {result.AccessibilityJsonPath}");
            Console.WriteLine($"unsupported-apis.json: {result.UnsupportedApisJsonPath}");
            Console.WriteLine($"snapshot.json: {result.SnapshotJsonPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static ISnapshotRenderer CreateSnapshotRenderer(string rendererName)
    {
        return rendererName.ToLowerInvariant() switch
        {
            "svg" => new SnapshotRenderer(),
            "skia" => new SkiaSnapshotRenderer(),
            _ => throw new ArgumentException($"Unknown renderer '{rendererName}'. Expected 'svg' or 'skia'.")
        };
    }

    private static int RunXaml(string[] args)
    {
        if (args.Length == 0 || args[0] != "compile")
        {
            Console.Error.WriteLine("Expected xaml compile.");
            return 2;
        }

        var outputPath = ReadOption(args, "--output");
        if (outputPath is null)
        {
            Console.Error.WriteLine("Missing required option: --output <path>");
            return 2;
        }

        var inputFiles = ReadPositionalInputs(args[1..]);
        if (inputFiles.Count == 0)
        {
            Console.Error.WriteLine("At least one XAML input file is required.");
            return 2;
        }

        var compiler = new MacXamlCompiler();
        var generatedSources = new List<string>();
        var diagnostics = new List<XamlDiagnostic>();

        foreach (var inputFile in inputFiles)
        {
            var result = compiler.CompileFile(inputFile);
            diagnostics.AddRange(result.Diagnostics);
            if (!string.IsNullOrWhiteSpace(result.GeneratedSource))
            {
                generatedSources.Add(result.GeneratedSource);
            }
        }

        var diagnosticsPath = Path.ChangeExtension(outputPath, ".diagnostics.json");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(diagnosticsPath, JsonSerializer.Serialize(diagnostics, JsonDefaults.Options));

        if (diagnostics.Any(diagnostic => diagnostic.Severity == "Error"))
        {
            Console.Error.WriteLine($"XAML compilation failed. Diagnostics: {diagnosticsPath}");
            return 1;
        }

        File.WriteAllText(outputPath, string.Join(Environment.NewLine, generatedSources));
        Console.WriteLine($"Generated XAML source: {outputPath}");
        return 0;
    }

    private static string? ReadOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index] == name)
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ReadPositionalInputs(string[] args)
    {
        var values = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            var value = args[index];
            if (value.StartsWith("--", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            values.Add(value);
        }

        return values;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("WinUI3 Mac Test Runtime");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  doctor [--json]");
        Console.WriteLine("  run --project <path> [--configuration Debug] [--output <path>] [--script <path>] [--renderer svg|skia]");
        Console.WriteLine("  xaml compile --output <path> <xaml-file> [...]");
    }
}
