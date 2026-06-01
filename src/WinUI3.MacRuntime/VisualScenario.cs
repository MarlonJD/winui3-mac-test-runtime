using System.Globalization;
using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed class VisualScenario
{
    public string SchemaVersion { get; init; } = "0.1";

    public string FixtureName { get; init; } = "fixture";

    public string Name { get; init; } = "default";

    public VisualViewport Viewport { get; init; } = new(1280, 800);

    public double Scale { get; init; } = 1.0;

    public string Theme { get; init; } = "light";

    public string? StartupRoute { get; init; }

    public bool StrictVisual { get; init; }

    public VisualThresholds Thresholds { get; init; } = new();

    public IReadOnlyList<InteractionAction> Interactions { get; init; } = Array.Empty<InteractionAction>();

    public IReadOnlyList<VisualRequirement> Requirements { get; init; } = Array.Empty<VisualRequirement>();

    public static async Task<VisualScenario> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        var scenario = await JsonSerializer.DeserializeAsync<VisualScenario>(stream, JsonDefaults.Options, cancellationToken);
        if (scenario is null)
        {
            throw new InvalidOperationException($"Scenario '{path}' did not contain a valid JSON object.");
        }

        scenario.Validate(path);
        return scenario;
    }

    private void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(FixtureName))
        {
            throw new InvalidOperationException($"Scenario '{path}' must declare fixtureName.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Scenario '{path}' must declare name.");
        }

        if (Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            throw new InvalidOperationException($"Scenario '{path}' must use a positive viewport.");
        }

        if (Scale <= 0)
        {
            throw new InvalidOperationException($"Scenario '{path}' must use a positive scale.");
        }

        if (!VisualTheme.IsSupported(Theme))
        {
            throw new InvalidOperationException($"Scenario '{path}' uses unsupported theme '{Theme}'. Expected light or dark.");
        }
    }
}

public sealed record VisualViewport(int Width, int Height)
{
    public static VisualViewport Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('x', 'X');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var width) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var height) ||
            width <= 0 ||
            height <= 0)
        {
            throw new FormatException($"Viewport '{value}' must use <width>x<height>, for example 1280x800.");
        }

        return new VisualViewport(width, height);
    }

    public override string ToString()
    {
        return Width.ToString(CultureInfo.InvariantCulture) + "x" + Height.ToString(CultureInfo.InvariantCulture);
    }
}

public sealed class VisualThresholds
{
    public double ChangedPixelPercentage { get; init; } = 1.0;

    public int MaxChannelDelta { get; init; } = 255;

    public double MeanAbsoluteError { get; init; } = 8.0;

    public double RootMeanSquaredError { get; init; } = 16.0;
}

public sealed record VisualRequirement(
    string Control,
    IReadOnlyList<string> Properties);

public sealed record VisualRunSettings(
    VisualScenario? Scenario,
    string ScenarioName,
    string Renderer,
    VisualViewport Viewport,
    double Scale,
    string Theme,
    bool StrictVisual,
    VisualThresholds Thresholds);

public sealed record SnapshotRenderOptions(
    string Renderer,
    string? ScenarioName,
    VisualViewport Viewport,
    double Scale,
    string Theme,
    bool StrictVisual,
    string? PreferredFileName = null);

public static class VisualTheme
{
    public static bool IsSupported(string? theme)
    {
        return string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? theme)
    {
        return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
    }
}
