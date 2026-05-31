using System.Runtime.CompilerServices;

namespace Microsoft.UI.Xaml.Automation;

public static class AutomationProperties
{
    private static readonly ConditionalWeakTable<DependencyObject, AutomationMetadata> Metadata = new();

    public static void SetName(DependencyObject element, string? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        Metadata.GetOrCreateValue(element).Name = value;
    }

    public static string? GetName(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return Metadata.TryGetValue(element, out var metadata) ? metadata.Name : null;
    }

    public static void SetHelpText(DependencyObject element, string? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        Metadata.GetOrCreateValue(element).HelpText = value;
    }

    public static string? GetHelpText(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return Metadata.TryGetValue(element, out var metadata) ? metadata.HelpText : null;
    }

    private sealed class AutomationMetadata
    {
        public string? Name { get; set; }

        public string? HelpText { get; set; }
    }
}
