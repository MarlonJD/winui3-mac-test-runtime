using WinUI3.MacRunner.ProjectIngestion;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class ProjectIngestionTests
{
    [TestMethod]
    public async Task ProjectIngestionInspectorReadsWindowsWinUIAppShape()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-project-ingestion-tests", Guid.NewGuid().ToString("N"));
        var projectPath = await WriteDirectAppFixtureAsync(root, windowsPackageType: "MSIX");

        var model = WinUIProjectInspector.Inspect(projectPath);

        Assert.AreEqual(projectPath, model.ProjectPath);
        Assert.AreEqual(root, model.RootDirectory);
        Assert.AreEqual("net10.0-windows10.0.19041.0", model.TargetFramework);
        Assert.IsTrue(model.UseWinUI);
        Assert.AreEqual("MSIX", model.WindowsPackageType);
        Assert.IsTrue(model.PackageReferences.Any(package => package.Include == "Microsoft.WindowsAppSDK"));
        Assert.IsTrue(model.ProjectReferences.Any(reference => reference.Include.EndsWith("SharedLogic.csproj", StringComparison.Ordinal)));
        Assert.AreEqual("App.xaml", model.ApplicationXaml?.RelativePath);
        Assert.IsTrue(model.PageXamlFiles.Any(file => file.RelativePath == "MainWindow.xaml"));
        Assert.IsTrue(model.PageXamlFiles.Any(file => file.RelativePath == "Pages/HomePage.xaml"));
        Assert.IsTrue(model.ResourceDictionaryXamlFiles.Any(file => file.RelativePath == "Themes/Tokens.xaml"));
        Assert.IsTrue(model.ContentAssets.Any(file => file.RelativePath == "Assets/PublicLogo.svg"));
    }

    [TestMethod]
    public void DirectAppInspectionReadsMeetingChallengeWhenPresent()
    {
        var projectPath = "/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj";
        if (!File.Exists(projectPath))
        {
            Assert.Inconclusive($"Downstream validation project is not present: {projectPath}");
        }

        var model = WinUIProjectInspector.Inspect(projectPath);

        Assert.AreEqual("net10.0-windows10.0.19041.0", model.TargetFramework);
        Assert.IsTrue(model.UseWinUI);
        Assert.AreEqual("MSIX", model.WindowsPackageType);
        Assert.AreEqual("App.xaml", model.ApplicationXaml?.RelativePath);
        Assert.IsTrue(model.PageXamlFiles.Any(file => file.RelativePath == "MainWindow.xaml"));
        Assert.IsTrue(model.PageXamlFiles.Any(file => file.RelativePath.StartsWith("Pages/", StringComparison.Ordinal)));
        Assert.IsTrue(model.ResourceDictionaryXamlFiles.Any(file => file.RelativePath.StartsWith("Themes/", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task DirectAppGeneratedHostWriterEmitsMeetingChallengeWhenPresent()
    {
        var projectPath = "/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj";
        if (!File.Exists(projectPath))
        {
            Assert.Inconclusive($"Downstream validation project is not present: {projectPath}");
        }

        var model = WinUIProjectInspector.Inspect(projectPath);

        var host = await GeneratedHostWriter.WriteAsync(model, new GeneratedHostOptions());

        StringAssert.StartsWith(host.RootDirectory, "/private/tmp/winui3-mac-test-runtime/generated-hosts/");
        Assert.IsTrue(File.Exists(host.ProjectPath));
        var projectText = await File.ReadAllTextAsync(host.ProjectPath);
        StringAssert.Contains(projectText, "MainWindow.xaml");
        StringAssert.Contains(projectText, "Pages/");
        StringAssert.Contains(projectText, "Themes/");
    }

    [TestMethod]
    public async Task GeneratedHostWriterWritesTemporarySourceLevelHostUnderPrivateTmp()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-project-ingestion-tests", Guid.NewGuid().ToString("N"));
        var projectPath = await WriteDirectAppFixtureAsync(sourceRoot, windowsPackageType: "MSIX");
        var model = WinUIProjectInspector.Inspect(projectPath);
        var sourceStamp = Directory.GetFileSystemEntries(sourceRoot, "*", SearchOption.AllDirectories).Length;

        var host = await GeneratedHostWriter.WriteAsync(model, new GeneratedHostOptions());

        StringAssert.StartsWith(host.RootDirectory, "/private/tmp/winui3-mac-test-runtime/generated-hosts/");
        Assert.IsTrue(File.Exists(host.ProjectPath));
        Assert.IsTrue(File.Exists(Path.Combine(host.RootDirectory, "App.xaml")));
        Assert.IsTrue(File.Exists(Path.Combine(host.RootDirectory, "App.xaml.cs")));
        var projectText = await File.ReadAllTextAsync(host.ProjectPath);
        StringAssert.Contains(projectText, "TargetFramework>net10.0</TargetFramework");
        StringAssert.Contains(projectText, "WinUI3.MacCompat");
        StringAssert.Contains(projectText, "WinUI3.MacRuntime");
        StringAssert.Contains(projectText, "WinUI3MacXaml");
        StringAssert.Contains(projectText, "Pages/HomePage.xaml");
        StringAssert.Contains(projectText, "Themes/Tokens.xaml");
        Assert.AreEqual(sourceStamp, Directory.GetFileSystemEntries(sourceRoot, "*", SearchOption.AllDirectories).Length);
    }

    [TestMethod]
    public async Task ProjectIngestionReportDocumentsGeneratedHostOutput()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-project-ingestion-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-project-ingestion-output", Guid.NewGuid().ToString("N"));
        var projectPath = await WriteDirectAppFixtureAsync(sourceRoot, windowsPackageType: "None");

        var plan = await new ProjectIngestionService().PrepareAsync(projectPath, outputRoot, "Debug");

        Assert.IsNotNull(plan.Report);
        StringAssert.StartsWith(plan.Report.GeneratedHostPath, "/private/tmp/winui3-mac-test-runtime/generated-hosts/");
        Assert.IsTrue(File.Exists(plan.Report.GeneratedHostProjectPath));
        var reportJson = await File.ReadAllTextAsync(Path.Combine(outputRoot, "project-ingestion.json"));
        StringAssert.Contains(reportJson, "\"generatedHostPath\"");
        StringAssert.Contains(reportJson, "\"generatedHostProjectPath\"");
    }

    private static async Task<string> WriteDirectAppFixtureAsync(string root, string windowsPackageType)
    {
        Directory.CreateDirectory(Path.Combine(root, "Pages"));
        Directory.CreateDirectory(Path.Combine(root, "Themes"));
        Directory.CreateDirectory(Path.Combine(root, "Assets"));
        Directory.CreateDirectory(Path.Combine(root, "Shared"));

        await File.WriteAllTextAsync(Path.Combine(root, "DirectFixture.csproj"), $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
                <UseWinUI>true</UseWinUI>
                <WindowsPackageType>{{windowsPackageType}}</WindowsPackageType>
                <AssemblyName>DirectFixture</AssemblyName>
                <RootNamespace>DirectFixture</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250401001" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="Shared\SharedLogic.csproj" />
              </ItemGroup>
              <ItemGroup>
                <ApplicationDefinition Include="App.xaml" />
                <Page Include="MainWindow.xaml" />
                <Page Include="Pages\HomePage.xaml" />
                <Page Include="Themes\Tokens.xaml" />
                <Content Include="Assets\PublicLogo.svg" />
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
                Title="Direct Fixture" />
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Pages", "HomePage.xaml"), """
            <Page
                x:Class="DirectFixture.Pages.HomePage"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="Home" />
            </Page>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Themes", "Tokens.xaml"), """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <x:String x:Key="PublicToken">Value</x:String>
            </ResourceDictionary>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Assets", "PublicLogo.svg"), "<svg />");
        await File.WriteAllTextAsync(Path.Combine(root, "Shared", "SharedLogic.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        return Path.Combine(root, "DirectFixture.csproj");
    }
}
