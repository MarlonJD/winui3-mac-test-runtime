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
}
