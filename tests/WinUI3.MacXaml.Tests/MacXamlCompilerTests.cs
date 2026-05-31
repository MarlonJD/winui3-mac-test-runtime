using WinUI3.MacXaml;

namespace WinUI3.MacXaml.Tests;

[TestClass]
public sealed class MacXamlCompilerTests
{
    [TestMethod]
    public void CompileTextGeneratesNamedControlsAndEventHookups()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Title="Sample">
              <StackPanel x:Name="RootStack">
                <TextBlock x:Name="GreetingText">Hello</TextBlock>
                <Button x:Name="PrimaryButton" Content="Continue" Click="OnClick" />
              </StackPanel>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "public sealed partial class MainWindow : Microsoft.UI.Xaml.Window");
        StringAssert.Contains(result.GeneratedSource, "public Microsoft.UI.Xaml.Controls.StackPanel RootStack");
        StringAssert.Contains(result.GeneratedSource, "__element2.Text = \"Hello\"");
        StringAssert.Contains(result.GeneratedSource, "__element3.Click += OnClick");
    }

    [TestMethod]
    public void CompileTextReportsLineDiagnosticsForUnsupportedElements()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <UnsupportedControl />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual("XAML1001", result.Diagnostics[0].Code);
        Assert.AreEqual("MainWindow.xaml", result.Diagnostics[0].FilePath);
        Assert.IsNotNull(result.Diagnostics[0].Line);
    }
}
