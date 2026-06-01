namespace ProductionSmoke.WinUI;

public sealed record NativeLaunchOptions(string ScenarioName)
{
    public static NativeLaunchOptions Default { get; } = new("production-smoke-light");

    public static NativeLaunchOptions Parse(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return Default;
        }

        var scenario = arguments
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => part.StartsWith("--scenario=", StringComparison.OrdinalIgnoreCase));
        return scenario is null
            ? Default
            : new NativeLaunchOptions(scenario["--scenario=".Length..]);
    }
}
