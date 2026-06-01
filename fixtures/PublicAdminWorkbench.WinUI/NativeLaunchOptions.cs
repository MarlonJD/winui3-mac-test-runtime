using System.Text;
using System.Text.Json;

namespace PublicAdminWorkbench.WinUI;

public sealed record NativeLaunchOptions(
    string? ScenarioPath,
    string ScenarioName,
    int ViewportWidth,
    int ViewportHeight,
    double Scale,
    string Theme,
    string? StartupRoute)
{
    public string WindowTitle => "WinUI3 Mac Test Runtime - " + ScenarioName;

    public static NativeLaunchOptions Default { get; } = new(
        ScenarioPath: null,
        ScenarioName: "public-admin-workbench-light",
        ViewportWidth: 1044,
        ViewportHeight: 720,
        Scale: 1.0,
        Theme: "light",
        StartupRoute: "queue");

    public static NativeLaunchOptions Parse(string? launchArguments)
    {
        var tokens = string.IsNullOrWhiteSpace(launchArguments)
            ? Environment.GetCommandLineArgs().Skip(1).ToArray()
            : TokenizeArguments(launchArguments).ToArray();
        var values = ReadOptions(tokens);
        var scenarioPath = values.GetValueOrDefault("--scenario");
        var scenarioName = values.GetValueOrDefault("--scenario-name");
        var viewportWidth = 1044;
        var viewportHeight = 720;
        var scale = 1.0;
        var theme = "light";
        string? startupRoute = null;

        if (!string.IsNullOrWhiteSpace(scenarioPath) && File.Exists(scenarioPath))
        {
            using var scenario = JsonDocument.Parse(File.ReadAllText(scenarioPath));
            var root = scenario.RootElement;
            scenarioName ??= ReadString(root, "name") ?? Path.GetFileNameWithoutExtension(scenarioPath);
            theme = ReadString(root, "theme") ?? theme;
            startupRoute = ReadString(root, "startupRoute");
            scale = ReadDouble(root, "scale") ?? scale;

            if (root.TryGetProperty("viewport", out var viewport))
            {
                viewportWidth = ReadInt(viewport, "width") ?? viewportWidth;
                viewportHeight = ReadInt(viewport, "height") ?? viewportHeight;
            }
        }

        return new NativeLaunchOptions(
            scenarioPath,
            string.IsNullOrWhiteSpace(scenarioName) ? "public-admin-workbench-light" : scenarioName,
            viewportWidth,
            viewportHeight,
            scale,
            string.IsNullOrWhiteSpace(theme) ? "light" : theme,
            startupRoute);
    }

    private static Dictionary<string, string> ReadOptions(IReadOnlyList<string> args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            if (option is "--scenario" or "--scenario-name")
            {
                if (index + 1 >= args.Count)
                {
                    throw new ArgumentException($"Missing value for {option}.");
                }

                values[option] = args[++index];
            }
        }

        return values;
    }

    private static IEnumerable<string> TokenizeArguments(string arguments)
    {
        var token = new StringBuilder();
        var inQuotes = false;
        foreach (var character in arguments)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                if (token.Length > 0)
                {
                    yield return token.ToString();
                    token.Clear();
                }

                continue;
            }

            token.Append(character);
        }

        if (token.Length > 0)
        {
            yield return token.ToString();
        }
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetDouble(out var value)
            ? value
            : null;
    }
}
