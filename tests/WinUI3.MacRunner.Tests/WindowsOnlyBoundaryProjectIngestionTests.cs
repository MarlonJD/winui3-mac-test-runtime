using System.Text.Json;
using WinUI3.MacRunner.ProjectIngestion;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class WindowsOnlyBoundaryProjectIngestionTests
{
    [TestMethod]
    public async Task ClassifierDetectsWindowsStorageApplicationDataBoundary()
    {
        var root = NewSourceRoot();
        var projectPath = await WriteWindowsOnlyFixtureAsync(root, windowsPackageType: "None");
        var model = WinUIProjectInspector.Inspect(projectPath);

        var boundaries = WindowsOnlyBoundaryClassifier.Classify(model);

        var storage = boundaries.SingleOrDefault(boundary => boundary.Api == "Windows.Storage.ApplicationData");
        Assert.IsNotNull(storage, "Expected a Windows.Storage.ApplicationData boundary.");
        Assert.AreEqual("windows-only", storage.Status);
        Assert.AreEqual("Storage/WindowsSecureSessionStore.cs", storage.FilePath);
        Assert.IsTrue(storage.Line > 0);
        Assert.IsFalse(storage.BlocksRender);
    }

    [TestMethod]
    public async Task ClassifierDetectsPasswordVaultCredentialBoundary()
    {
        var root = NewSourceRoot();
        var projectPath = await WriteWindowsOnlyFixtureAsync(root, windowsPackageType: "None");
        var model = WinUIProjectInspector.Inspect(projectPath);

        var boundaries = WindowsOnlyBoundaryClassifier.Classify(model);

        var credentials = boundaries.SingleOrDefault(boundary =>
            boundary.Api == "Windows.Security.Credentials.PasswordVault");
        Assert.IsNotNull(credentials, "Expected a Windows.Security.Credentials.PasswordVault boundary.");
        Assert.AreEqual("windows-only", credentials.Status);
        Assert.AreEqual("Storage/WindowsSecureSessionStore.cs", credentials.FilePath);
        Assert.IsFalse(credentials.BlocksRender);
    }

    [TestMethod]
    public async Task ClassifierDetectsSystemBackdropAndMicaBoundaries()
    {
        var root = NewSourceRoot();
        var projectPath = await WriteWindowsOnlyFixtureAsync(root, windowsPackageType: "None");
        var model = WinUIProjectInspector.Inspect(projectPath);

        var boundaries = WindowsOnlyBoundaryClassifier.Classify(model);

        var backdrop = boundaries.Where(boundary => boundary.Boundary == "system-backdrop").ToArray();
        Assert.IsTrue(
            backdrop.Any(boundary => boundary.Api == "Microsoft.UI.Xaml.Media.MicaBackdrop"),
            "Expected a MicaBackdrop boundary.");
        Assert.IsTrue(
            backdrop.Any(boundary => boundary.Api == "Microsoft.UI.Xaml.Media.SystemBackdrop"),
            "Expected a SystemBackdrop boundary.");
        // Backdrops are cataloged as planned, not windows-only.
        Assert.IsTrue(backdrop.All(boundary => boundary.Status == "planned"));
        Assert.IsTrue(backdrop.All(boundary => !boundary.BlocksRender));
    }

    [TestMethod]
    public async Task ClassifierDetectsWindowsAppSdkDeploymentBoundary()
    {
        var root = NewSourceRoot();
        var projectPath = await WriteWindowsOnlyFixtureAsync(root, windowsPackageType: "None");
        var model = WinUIProjectInspector.Inspect(projectPath);

        var boundaries = WindowsOnlyBoundaryClassifier.Classify(model);

        var deployment = boundaries.Where(boundary => boundary.Boundary == "windows-app-sdk-deployment").ToArray();
        Assert.IsTrue(
            deployment.Any(boundary => boundary.Api == "Microsoft.WindowsAppSDK"),
            "Expected a Microsoft.WindowsAppSDK deployment boundary.");
        Assert.IsTrue(deployment.All(boundary => boundary.Status == "windows-only"));
        Assert.IsTrue(deployment.All(boundary => !boundary.BlocksRender));
    }

    [TestMethod]
    public async Task ClassifierDetectsPackagedActivationBoundaryFromPackageType()
    {
        var root = NewSourceRoot();
        var projectPath = await WriteWindowsOnlyFixtureAsync(root, windowsPackageType: "MSIX");
        var model = WinUIProjectInspector.Inspect(projectPath);

        var boundaries = WindowsOnlyBoundaryClassifier.Classify(model);

        var activation = boundaries.Where(boundary => boundary.Boundary == "packaged-activation").ToArray();
        Assert.IsTrue(activation.Length > 0, "Expected a packaged-activation boundary for WindowsPackageType=MSIX.");
        Assert.IsTrue(activation.All(boundary => boundary.Status == "windows-only"));
        Assert.IsTrue(activation.All(boundary => !boundary.BlocksRender));
    }

    [TestMethod]
    public async Task BoundaryDiagnosticsNeverBlockRenderableOutput()
    {
        var root = NewSourceRoot();
        var projectPath = await WriteWindowsOnlyFixtureAsync(root, windowsPackageType: "MSIX");
        var model = WinUIProjectInspector.Inspect(projectPath);

        var boundaries = WindowsOnlyBoundaryClassifier.Classify(model);

        Assert.IsTrue(boundaries.Count > 0);
        Assert.IsTrue(
            boundaries.All(boundary => !boundary.BlocksRender),
            "Windows-only boundary diagnostics must not block renderable XAML/page output.");
    }

    [TestMethod]
    public async Task ProjectIngestionReportRecordsWindowsOnlyBoundariesWithoutBlocking()
    {
        var sourceRoot = NewSourceRoot();
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            "winui3-mac-runner-project-ingestion-output",
            Guid.NewGuid().ToString("N"));
        var projectPath = await WriteWindowsOnlyFixtureAsync(sourceRoot, windowsPackageType: "None");

        var plan = await new ProjectIngestionService().PrepareAsync(projectPath, outputRoot, "Debug");

        Assert.IsNotNull(plan.Report);
        Assert.AreEqual(ProjectIngestionStatuses.Passed, plan.Report.Status);
        Assert.IsTrue(
            plan.Report.WindowsOnlyBoundaries.Any(boundary =>
                boundary.Api == "Windows.Security.Credentials.PasswordVault"),
            "project-ingestion.json should record the PasswordVault Windows-only boundary.");
        Assert.IsTrue(
            plan.Report.WindowsOnlyBoundaries.All(boundary => !boundary.BlocksRender));

        using var document = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(outputRoot, "project-ingestion.json")));
        var boundaries = document.RootElement.GetProperty("windowsOnlyBoundaries").EnumerateArray().ToArray();
        Assert.IsTrue(boundaries.Any(boundary =>
            boundary.GetProperty("api").GetString() == "Windows.Storage.ApplicationData"));
    }

    [TestMethod]
    public void MeetingChallengeBoundariesAreClassifiedWhenPresent()
    {
        var projectPath =
            "/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj";
        if (!File.Exists(projectPath))
        {
            Assert.Inconclusive($"Downstream validation project is not present: {projectPath}");
        }

        var model = WinUIProjectInspector.Inspect(projectPath);

        var boundaries = WindowsOnlyBoundaryClassifier.Classify(model);

        Assert.IsTrue(boundaries.Any(boundary => boundary.Api == "Windows.Storage.ApplicationData"));
        Assert.IsTrue(boundaries.Any(boundary => boundary.Api == "Windows.Security.Credentials.PasswordVault"));
        Assert.IsTrue(boundaries.Any(boundary => boundary.Boundary == "system-backdrop"));
        Assert.IsTrue(boundaries.All(boundary => !boundary.BlocksRender));
    }

    private static string NewSourceRoot()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "winui3-mac-runner-windows-only-boundary-tests",
            Guid.NewGuid().ToString("N"));
    }

    private static async Task<string> WriteWindowsOnlyFixtureAsync(string root, string windowsPackageType)
    {
        Directory.CreateDirectory(Path.Combine(root, "Pages"));
        Directory.CreateDirectory(Path.Combine(root, "Storage"));

        await File.WriteAllTextAsync(Path.Combine(root, "WindowsOnlyFixture.csproj"), $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
                <UseWinUI>true</UseWinUI>
                <WindowsPackageType>{{windowsPackageType}}</WindowsPackageType>
                <WindowsAppSDKSelfContained>false</WindowsAppSDKSelfContained>
                <AssemblyName>WindowsOnlyFixture</AssemblyName>
                <RootNamespace>WindowsOnlyFixture</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250401001" />
                <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1742" />
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
                x:Class="WindowsOnlyFixture.App"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "MainWindow.xaml"), """
            <Window
                x:Class="WindowsOnlyFixture.MainWindow"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Title="Windows-only fixture" />
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "MainWindow.xaml.cs"), """
            using Microsoft.UI.Xaml;
            using Microsoft.UI.Xaml.Media;

            namespace WindowsOnlyFixture;

            public sealed partial class MainWindow : Window
            {
                public MainWindow()
                {
                    InitializeComponent();
                    SystemBackdrop = new MicaBackdrop();
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Pages", "HomePage.xaml"), """
            <Page
                x:Class="WindowsOnlyFixture.Pages.HomePage"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="Home" />
            </Page>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Storage", "WindowsSecureSessionStore.cs"), """
            using Windows.Security.Credentials;
            using Windows.Storage;

            namespace WindowsOnlyFixture.Storage;

            public sealed class WindowsSecureSessionStore
            {
                private readonly PasswordVault vault = new();
                private readonly ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
            }
            """);

        return Path.Combine(root, "WindowsOnlyFixture.csproj");
    }
}
