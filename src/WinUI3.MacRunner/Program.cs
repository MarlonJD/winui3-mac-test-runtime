using System.Text.Json;
using WinUI3.MacRuntime;

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

        try
        {
            var runner = new MacProjectRunner();
            var result = await runner.RunProjectAsync(projectPath, outputDirectory, configuration);
            Console.WriteLine($"Status: {result.Run.Status}");
            Console.WriteLine($"run.json: {result.RunJsonPath}");
            Console.WriteLine($"tree.json: {result.TreeJsonPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
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
        Console.WriteLine("  run --project <path> [--configuration Debug] [--output <path>]");
    }
}
