using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI3.MacRuntime;

public static class ElementQuery
{
    public static object? FindByName(object root, string name)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Traverse(root).FirstOrDefault(element =>
            element is FrameworkElement frameworkElement &&
            frameworkElement.Name == name);
    }

    public static IReadOnlyList<object> Traverse(object root)
    {
        var values = new List<object>();
        Visit(root, values);
        return values;
    }

    private static void Visit(object? element, ICollection<object> values)
    {
        if (element is null)
        {
            return;
        }

        values.Add(element);
        foreach (var child in EnumerateChildren(element))
        {
            Visit(child, values);
        }
    }

    private static IEnumerable<object?> EnumerateChildren(object element)
    {
        return element switch
        {
            Window window => One(window.Content),
            Page page => One(page.Content),
            UserControl userControl => One(userControl.Content),
            ContentControl contentControl => One(contentControl.Content),
            ScrollViewer scrollViewer => One(scrollViewer.Content),
            Border border => One(border.Child),
            Button button => One(button.Content),
            Frame frame => One(frame.Content),
            StackPanel stackPanel => stackPanel.Children,
            Grid grid => grid.Children,
            NavigationView navigationView => navigationView.MenuItems.Concat(One(navigationView.PaneFooter)).Concat(One(navigationView.Content)),
            NavigationViewItem item => One(item.Content).Concat(One(item.Icon)),
            CommandBar commandBar => commandBar.PrimaryCommands,
            ItemsControl itemsControl => itemsControl.Items,
            _ => Array.Empty<object?>()
        };
    }

    private static IEnumerable<object?> One(object? value)
    {
        if (value is not null)
        {
            yield return value;
        }
    }
}
