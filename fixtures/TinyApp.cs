using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TinyWinUIApp.MacTest;

public sealed class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var root = new StackPanel { Name = "RootStack" };
        root.Children.Add(new TextBlock
        {
            Name = "GreetingText",
            Text = "Hello from a Wine-free WinUI facade"
        });
        root.Children.Add(new Button
        {
            Name = "PrimaryButton",
            Content = "Continue"
        });

        MainWindow = new Window
        {
            Title = "Tiny WinUI App",
            Content = root
        };
    }
}
