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
    public void CompileTextGeneratesBindingRegistrations()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock x:Name="TitleText" Text="{Binding Title}" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Data.BindingOperations.SetBinding");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Data.Binding(\"Title\", Microsoft.UI.Xaml.Data.BindingMode.OneWay)");
    }

    [TestMethod]
    public void CompileTextGeneratesPathBindingRegistrations()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock x:Name="TitleText" Text="{Binding Path=Title, Mode=OneWay}" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Data.Binding(\"Title\", Microsoft.UI.Xaml.Data.BindingMode.OneWay)");
    }

    [TestMethod]
    public void CompileTextGeneratesTwoWayBindingRegistrations()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBox x:Name="TitleBox" Text="{Binding Path=Title, Mode=TwoWay}" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Data.Binding(\"Title\", Microsoft.UI.Xaml.Data.BindingMode.TwoWay)");
    }

    [TestMethod]
    public void CompileTextGeneratesAutomationPropertyRegistrations()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Button x:Name="PrimaryButton" AutomationProperties.Name="Primary action" AutomationProperties.HelpText="Runs it" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Automation.AutomationProperties.SetName");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText");
    }

    [TestMethod]
    public void CompileTextGeneratesUidAndGridColumnRegistrations()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Grid>
                <StackPanel x:Name="DetailPanel" x:Uid="DetailPanel" Grid.Column="1" />
              </Grid>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, ".Uid = \"DetailPanel\"");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Controls.Grid.SetColumn(__element2, 1)");
    }

    [TestMethod]
    public void CompileTextGeneratesResourceFailureAwareLookups()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="{StaticResource MissingTitle}" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.ResourceOperations.ResolveString");
        StringAssert.Contains(result.GeneratedSource, "\"MissingTitle\"");
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

    [TestMethod]
    public void CompileTextReportsUnsupportedProperties()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Button Flyout="Unsupported" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1002", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "Flyout");
        StringAssert.Contains(result.Diagnostics[0].Message, "cataloged as partial");
        Assert.IsNotNull(result.Diagnostics[0].Line);
    }

    [TestMethod]
    public void CompileTextReportsUnknownCatalogGapsForUnsupportedProperties()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Button FuturePublicProperty="Tracked by diagnostics" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1002", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "not present in the WinUI compatibility catalog");
    }

    [TestMethod]
    public void CompileTextReportsCatalogStatusForMaterialElements()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <MicaBackdrop />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1001", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "cataloged as planned");
    }

    [TestMethod]
    public void CompileTextReportsUnsupportedPropertyElements()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <StackPanel>
                <StackPanel.ChildrenTransitions>
                  <Button />
                </StackPanel.ChildrenTransitions>
              </StackPanel>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1003", result.Diagnostics[0].Code);
        Assert.IsNotNull(result.Diagnostics[0].Line);
    }

    [TestMethod]
    public void CompileTextReportsCatalogStatusForVisualStatePropertyElements()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Grid>
                <VisualStateManager.VisualStateGroups>
                  <VisualState />
                </VisualStateManager.VisualStateGroups>
              </Grid>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1003", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "cataloged as planned");
    }

    [TestMethod]
    public void CompileTextGeneratesLevel2Controls()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <StackPanel>
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                  <StackPanel>
                    <CheckBox Content="Enabled" IsChecked="True" />
                    <RadioButton Content="High priority" GroupName="Priority" IsChecked="True" />
                    <ComboBox x:Name="StatusComboBox" PlaceholderText="Status" SelectedIndex="0" />
                    <ProgressBar Minimum="0" Maximum="100" Value="65" />
                    <ProgressRing IsActive="True" />
                    <InfoBar Title="Ready" Message="Public fixture state" Severity="Success" IsOpen="True" />
                    <CommandBar>
                      <CommandBar.PrimaryCommands>
                        <AppBarButton Label="Save" Click="OnSaveClicked" />
                      </CommandBar.PrimaryCommands>
                    </CommandBar>
                  </StackPanel>
                </ScrollViewer>
              </StackPanel>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.CheckBox()");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto");
        StringAssert.Contains(result.GeneratedSource, ".PrimaryCommands.Add(");
    }

    [TestMethod]
    public void CompileTextGeneratesStyleResourcesAndApplication()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Window.Resources>
                <ResourceDictionary>
                  <String x:Key="AccentBrush">#2562D9</String>
                  <Style x:Key="CommandTextStyle" TargetType="Button">
                    <Setter Property="Foreground" Value="{ThemeResource AccentBrush}" />
                  </Style>
                </ResourceDictionary>
              </Window.Resources>
              <Button Style="{StaticResource CommandTextStyle}" Content="Save" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Style");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Setter(\"Foreground\"");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.StyleOperations.Apply");
    }

    [TestMethod]
    public void CompileTextGeneratesApplicationRoot()
    {
        const string xaml = """
            <Application
                x:Class="Sample.App"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
            </Application>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "public sealed partial class App : Microsoft.UI.Xaml.Application");
    }

    [TestMethod]
    public void CompileTextAcceptsXamlControlsResourcesAndThemeDictionariesMarkers()
    {
        const string xaml = """
            <Application
                x:Class="Sample.App"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:controls="using:Microsoft.UI.Xaml.Controls">
              <Application.Resources>
                <ResourceDictionary>
                  <ResourceDictionary.MergedDictionaries>
                    <controls:XamlControlsResources />
                  </ResourceDictionary.MergedDictionaries>
                  <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key="Light">
                      <SolidColorBrush x:Key="AccentBrush" Color="#2562D9" />
                    </ResourceDictionary>
                    <ResourceDictionary x:Key="Dark">
                      <SolidColorBrush x:Key="AccentBrush" Color="#7CA7FF" />
                    </ResourceDictionary>
                  </ResourceDictionary.ThemeDictionaries>
                  <String x:Key="AccentBrush">#2562D9</String>
                </ResourceDictionary>
              </Application.Resources>
            </Application>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "\"XamlControlsResources\"");
        StringAssert.Contains(result.GeneratedSource, "\"ResourceDictionary.ThemeDictionaries\"");
        StringAssert.Contains(result.GeneratedSource, ".ThemeDictionaries[\"Dark\"]");
        StringAssert.Contains(result.GeneratedSource, "\"#7CA7FF\"");
    }

    [TestMethod]
    public void CompileTextGeneratesFrameContentAndListViewItems()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Frame>
                <Frame.Content>
                  <ListView SelectedIndex="0">
                    <ListView.Items>
                      <TextBlock Text="Review access request" />
                    </ListView.Items>
                  </ListView>
                </Frame.Content>
              </Frame>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, ".Content = __element2");
        StringAssert.Contains(result.GeneratedSource, ".Items.Add(__element3)");
        StringAssert.Contains(result.GeneratedSource, ".SelectedIndex = 0");
    }
}
