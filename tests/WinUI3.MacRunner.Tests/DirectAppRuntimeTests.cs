using System.Text.Json;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DirectAppRuntimeTests
{
    [TestMethod]
    public async Task DirectAppPageEntryRendersSelectedPageAndWritesAutomationArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-tests", Guid.NewGuid().ToString("N"));
        var projectPath = await WriteDirectAppFixtureAsync(root);
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-output", Guid.NewGuid().ToString("N"));
        var scenarioPath = Path.Combine(root, "direct-page-light.json");
        await File.WriteAllTextAsync(scenarioPath, """
            {
              "fixtureName": "direct-fixture",
              "name": "direct-page-light",
              "theme": "light",
              "entry": {
                "mode": "page",
                "xaml": "Pages/HomePage.xaml"
              },
              "automation": [
                { "type": "assertAccessibilityState", "target": "automationId=home-title", "key": "role", "parameter": "text" },
                { "type": "waitForIdle" }
              ],
              "visual": {
                "capture": true,
                "renderer": "skia-v2"
              }
            }
            """);
        var scenario = await VisualScenario.LoadAsync(scenarioPath);

        var result = await new MacProjectRunner(new SnapshotRenderer()).RunProjectAsync(
            projectPath,
            outputRoot,
            visualSettings: SettingsFor(scenario));

        Assert.AreEqual("passed", result.Run.Status);
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "tree.json")));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "accessibility.json")));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "interactions.json")));
        Assert.IsTrue(File.Exists(result.Snapshot.FilePath));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "project-ingestion.json")));
        AssertDirectIngestionReport(outputRoot, "Pages/HomePage.xaml");
        Assert.IsTrue(result.Tree.Root.Children.SelectMany(Flatten).Any(node => node.Properties.TryGetValue("automationId", out var id) && id?.ToString() == "home-title"));
        Assert.IsTrue(ReadInteractionStatuses(outputRoot).All(status => status == "passed"));
    }

    [TestMethod]
    public async Task DirectAppWindowEntryAppliesRouteAndRunsShellAutomation()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-tests", Guid.NewGuid().ToString("N"));
        var projectPath = await WriteDirectAppFixtureAsync(root);
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-output", Guid.NewGuid().ToString("N"));
        var scenarioPath = Path.Combine(root, "direct-shell-light.json");
        await File.WriteAllTextAsync(scenarioPath, """
            {
              "fixtureName": "direct-fixture",
              "name": "direct-shell-light",
              "theme": "light",
              "entry": {
                "mode": "window",
                "xaml": "MainWindow.xaml",
                "route": "home",
                "session": "staff"
              },
              "automation": [
                { "type": "assertAccessibilityState", "target": "automationId=shell-nav-home", "key": "selected", "parameter": "true" },
                { "type": "selectNavigation", "target": "automationId=shell-nav-messages" },
                { "type": "waitForIdle" },
                { "type": "assertProperty", "target": "ContentFrame", "key": "CurrentRoute", "parameter": "messages" },
                { "type": "assertAccessibilityState", "target": "automationId=shell-nav-messages", "key": "selected", "parameter": "true" }
              ],
              "visual": {
                "capture": true,
                "renderer": "skia-v2"
              }
            }
            """);
        var scenario = await VisualScenario.LoadAsync(scenarioPath);

        var result = await new MacProjectRunner(new SnapshotRenderer()).RunProjectAsync(
            projectPath,
            outputRoot,
            visualSettings: SettingsFor(scenario));

        Assert.AreEqual("passed", result.Run.Status);
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "tree.json")));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "accessibility.json")));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "interactions.json")));
        Assert.IsTrue(File.Exists(result.Snapshot.FilePath));
        AssertDirectIngestionReport(outputRoot, "MainWindow.xaml");
        Assert.IsTrue(ReadInteractionStatuses(outputRoot).All(status => status == "passed"));
    }

    [TestMethod]
    public async Task DirectAppPageEntryLoadsAppMergedThemeResources()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-tests", Guid.NewGuid().ToString("N"));
        var projectPath = await WriteResourceAppFixtureAsync(root);
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-output", Guid.NewGuid().ToString("N"));
        var scenario = new VisualScenario
        {
            FixtureName = "direct-resource-fixture",
            Name = "direct-resource-login-light",
            Theme = "light",
            Entry = new DirectAppEntry
            {
                Mode = "page",
                Xaml = "Pages/LoginPage.xaml"
            },
            Visual = new ScenarioVisualOptions
            {
                Capture = true,
                Renderer = "skia-v2"
            }
        };

        var result = await new MacProjectRunner(new SnapshotRenderer()).RunProjectAsync(
            projectPath,
            outputRoot,
            visualSettings: SettingsFor(scenario));

        Assert.AreEqual("passed", result.Run.Status);
        Assert.AreEqual(0, ReadResourceFailures(outputRoot).Length);
        var nodes = result.Tree.Root.Children.SelectMany(Flatten).ToArray();
        Assert.AreEqual("#f6f8fb", FindByName(nodes, "LoginScroll").Properties["background"]);
        Assert.AreEqual("#ffffff", FindByName(nodes, "LoginCard").Properties["background"]);
        Assert.AreEqual("#d6dde6", FindByName(nodes, "LoginCard").Properties["borderBrush"]);
    }

    [TestMethod]
    public async Task DirectAppPageEntryLoadsLocalizedUidTextFromResourceFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-tests", Guid.NewGuid().ToString("N"));
        var projectPath = await WriteResourceAppFixtureAsync(root);
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-direct-app-output", Guid.NewGuid().ToString("N"));
        var scenario = new VisualScenario
        {
            FixtureName = "direct-resource-fixture",
            Name = "direct-resource-login-light",
            Theme = "light",
            Entry = new DirectAppEntry
            {
                Mode = "page",
                Xaml = "Pages/LoginPage.xaml"
            }
        };

        var result = await new MacProjectRunner(new SnapshotRenderer()).RunProjectAsync(
            projectPath,
            outputRoot,
            visualSettings: SettingsFor(scenario));

        var nodes = result.Tree.Root.Children.SelectMany(Flatten).ToArray();
        Assert.AreEqual("Meeting Challenge", FindByName(nodes, "LoginTitleText").Properties["text"]);
        Assert.AreEqual("Sign in with your Meeting Challenge account.", FindByName(nodes, "LoginSubtitleText").Properties["text"]);
        Assert.AreEqual("Username", FindByName(nodes, "UsernameBox").Properties["placeholderText"]);
        Assert.AreEqual("Sign In", FindByName(nodes, "SignInButton").Properties["content"]);
    }

    private static void AssertDirectIngestionReport(string outputRoot, string expectedEntry)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputRoot, "project-ingestion.json")));
        Assert.IsFalse(document.RootElement.GetProperty("isShadowBuild").GetBoolean());
        var includedFiles = document.RootElement
            .GetProperty("includedFiles")
            .EnumerateArray()
            .Select(item => item.GetProperty("path").GetString())
            .ToArray();
        CollectionAssert.Contains(includedFiles, expectedEntry);
        CollectionAssert.DoesNotContain(includedFiles, "App.xaml.cs");
    }

    private static VisualRunSettings SettingsFor(VisualScenario scenario)
    {
        return new VisualRunSettings(
            Scenario: scenario,
            ScenarioName: scenario.Name,
            Renderer: scenario.Visual?.Renderer ?? "skia-v2",
            Viewport: scenario.Viewport,
            Scale: scenario.Scale,
            Theme: scenario.Theme,
            StrictVisual: true,
            Thresholds: scenario.Thresholds);
    }

    private static IEnumerable<UiNode> Flatten(UiNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static string[] ReadInteractionStatuses(string outputRoot)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputRoot, "interactions.json")));
        return document.RootElement
            .GetProperty("steps")
            .EnumerateArray()
            .Select(step => step.GetProperty("status").GetString() ?? string.Empty)
            .ToArray();
    }

    private static string[] ReadResourceFailures(string outputRoot)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputRoot, "resource-failures.json")));
        return document.RootElement
            .GetProperty("failures")
            .EnumerateArray()
            .Select(failure => failure.GetProperty("key").GetString() ?? string.Empty)
            .ToArray();
    }

    private static UiNode FindByName(IEnumerable<UiNode> nodes, string name)
    {
        return nodes.Single(node => node.Name == name);
    }

    private static async Task<string> WriteDirectAppFixtureAsync(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Pages"));
        await File.WriteAllTextAsync(Path.Combine(root, "DirectFixture.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
                <UseWinUI>true</UseWinUI>
                <WindowsPackageType>MSIX</WindowsPackageType>
                <AssemblyName>DirectFixture</AssemblyName>
                <RootNamespace>DirectFixture</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250401001" />
              </ItemGroup>
              <ItemGroup>
                <ApplicationDefinition Include="App.xaml" />
                <Page Include="MainWindow.xaml" />
                <Page Include="Pages\HomePage.xaml" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "App.xaml"), """
            <Application
                x:Class="DirectFixture.App"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "MainWindow.xaml"), """
            <Window
                x:Class="DirectFixture.MainWindow"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Title="Direct Fixture">
              <NavigationView x:Name="RootNavigation" SelectionChanged="OnNavigationSelectionChanged" Visibility="Collapsed">
                <NavigationView.MenuItems>
                  <NavigationViewItem x:Name="HomeNavigationItem" AutomationProperties.AutomationId="shell-nav-home" Content="Home" Tag="home" />
                  <NavigationViewItem x:Name="MessagesNavigationItem" AutomationProperties.AutomationId="shell-nav-messages" Content="Messages" Tag="messages" />
                </NavigationView.MenuItems>
                <Frame x:Name="ContentFrame" />
              </NavigationView>
            </Window>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Pages", "HomePage.xaml"), """
            <Page
                x:Class="DirectFixture.Pages.HomePage"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <StackPanel>
                <TextBlock x:Name="HomeTitle" AutomationProperties.AutomationId="home-title" Text="Direct home" />
              </StackPanel>
            </Page>
            """);

        return Path.Combine(root, "DirectFixture.csproj");
    }

    private static async Task<string> WriteResourceAppFixtureAsync(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Pages"));
        Directory.CreateDirectory(Path.Combine(root, "Themes"));
        Directory.CreateDirectory(Path.Combine(root, "Strings", "en-us"));
        await File.WriteAllTextAsync(Path.Combine(root, "DirectResourceFixture.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
                <UseWinUI>true</UseWinUI>
                <WindowsPackageType>MSIX</WindowsPackageType>
                <AssemblyName>DirectResourceFixture</AssemblyName>
                <RootNamespace>DirectResourceFixture</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250401001" />
              </ItemGroup>
              <ItemGroup>
                <ApplicationDefinition Include="App.xaml" />
                <Page Include="Pages\LoginPage.xaml" />
                <Page Include="Themes\Tokens.xaml" />
                <Page Include="Themes\Components.xaml" />
                <PRIResource Include="Strings\en-us\Resources.resw" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "App.xaml"), """
            <Application
                x:Class="DirectResourceFixture.App"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Application.Resources>
                    <ResourceDictionary>
                        <ResourceDictionary.MergedDictionaries>
                            <ResourceDictionary Source="Themes/Tokens.xaml" />
                            <ResourceDictionary Source="Themes/Components.xaml" />
                        </ResourceDictionary.MergedDictionaries>
                    </ResourceDictionary>
                </Application.Resources>
            </Application>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Themes", "Tokens.xaml"), """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key="Light">
                        <Color x:Key="EmsiAppBackgroundColor">#f6f8fb</Color>
                        <Color x:Key="EmsiSurfaceColor">#ffffff</Color>
                        <Color x:Key="EmsiBorderColor">#d6dde6</Color>
                        <Color x:Key="EmsiPrimaryTextColor">#17212b</Color>
                        <Color x:Key="EmsiSecondaryTextColor">#4f5c68</Color>
                        <SolidColorBrush x:Key="AppBackgroundBrush" Color="{StaticResource EmsiAppBackgroundColor}" />
                        <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource EmsiSurfaceColor}" />
                        <SolidColorBrush x:Key="SurfaceBorderBrush" Color="{StaticResource EmsiBorderColor}" />
                        <SolidColorBrush x:Key="PrimaryTextBrush" Color="{StaticResource EmsiPrimaryTextColor}" />
                        <SolidColorBrush x:Key="SecondaryTextBrush" Color="{StaticResource EmsiSecondaryTextColor}" />
                    </ResourceDictionary>
                    <ResourceDictionary x:Key="Dark">
                        <Color x:Key="EmsiAppBackgroundColor">#111418</Color>
                        <Color x:Key="EmsiSurfaceColor">#1a1d23</Color>
                        <Color x:Key="EmsiBorderColor">#3a414b</Color>
                        <Color x:Key="EmsiPrimaryTextColor">#f4f7fb</Color>
                        <Color x:Key="EmsiSecondaryTextColor">#a8b0ba</Color>
                        <SolidColorBrush x:Key="AppBackgroundBrush" Color="{StaticResource EmsiAppBackgroundColor}" />
                        <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource EmsiSurfaceColor}" />
                        <SolidColorBrush x:Key="SurfaceBorderBrush" Color="{StaticResource EmsiBorderColor}" />
                        <SolidColorBrush x:Key="PrimaryTextBrush" Color="{StaticResource EmsiPrimaryTextColor}" />
                        <SolidColorBrush x:Key="SecondaryTextBrush" Color="{StaticResource EmsiSecondaryTextColor}" />
                    </ResourceDictionary>
                </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Themes", "Components.xaml"), """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Style x:Key="PageTitleTextBlockStyle" TargetType="TextBlock">
                    <Setter Property="FontWeight" Value="SemiBold" />
                    <Setter Property="Foreground" Value="{ThemeResource PrimaryTextBrush}" />
                    <Setter Property="TextWrapping" Value="WrapWholeWords" />
                </Style>
                <Style x:Key="PageBodyTextBlockStyle" TargetType="TextBlock">
                    <Setter Property="Foreground" Value="{ThemeResource SecondaryTextBrush}" />
                    <Setter Property="TextWrapping" Value="Wrap" />
                </Style>
            </ResourceDictionary>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Pages", "LoginPage.xaml"), """
            <Page
                x:Class="DirectResourceFixture.Pages.LoginPage"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <ScrollViewer x:Name="LoginScroll" Background="{ThemeResource AppBackgroundBrush}" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
                    <Border
                        x:Name="LoginCard"
                        Background="{ThemeResource SurfaceBrush}"
                        BorderBrush="{ThemeResource SurfaceBorderBrush}"
                        BorderThickness="1"
                        CornerRadius="8">
                        <StackPanel>
                            <TextBlock x:Name="LoginTitleText" x:Uid="LoginTitle" Style="{StaticResource PageTitleTextBlockStyle}" />
                            <TextBlock x:Name="LoginSubtitleText" x:Uid="LoginSubtitle" Style="{StaticResource PageBodyTextBlockStyle}" />
                            <TextBox x:Name="UsernameBox" x:Uid="UsernameBox" />
                            <Button x:Name="SignInButton" x:Uid="SignInButton" />
                        </StackPanel>
                    </Border>
                </ScrollViewer>
            </Page>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Strings", "en-us", "Resources.resw"), """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="LoginTitle.Text" xml:space="preserve">
                <value>Meeting Challenge</value>
              </data>
              <data name="LoginSubtitle.Text" xml:space="preserve">
                <value>Sign in with your Meeting Challenge account.</value>
              </data>
              <data name="SignInButton.Content" xml:space="preserve">
                <value>Sign In</value>
              </data>
              <data name="UsernameBox.PlaceholderText" xml:space="preserve">
                <value>Username</value>
              </data>
            </root>
            """);

        return Path.Combine(root, "DirectResourceFixture.csproj");
    }
}
