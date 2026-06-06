using WinUI3.MacCompatibility;
using WinUI3.MacXaml;

namespace WinUI3.MacXaml.Tests;

[TestClass]
public sealed class MacXamlCompilerTests
{
    [TestMethod]
    public void CompatibilityCatalogClassifiesXamlConstructsUsedByCompiler()
    {
        var catalog = CompatibilityCatalog.Current;

        Assert.AreEqual(CompatibilityStatuses.Supported, catalog.FindXamlElement("Button")?.Status);
        Assert.AreEqual(CompatibilityStatuses.Planned, catalog.FindXamlElement("DataTemplate")?.Status);
        Assert.AreEqual(CompatibilityStatuses.Supported, catalog.FindXamlAttachedProperty("AutomationProperties.AutomationId")?.Status);
        Assert.AreEqual(CompatibilityStatuses.Planned, catalog.FindXamlDirective("x:Bind")?.Status);
        Assert.AreEqual(CompatibilityStatuses.Planned, catalog.FindXamlPropertyElement("ItemsControl", "ItemTemplate")?.Status);
    }

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
              <Button x:Name="PrimaryButton" AutomationProperties.AutomationId="primary-action" AutomationProperties.Name="Primary action" AutomationProperties.HelpText="Runs it" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId");
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
    public void CompileTextGeneratesGridRowDefinitionsSpacingAndSpanRegistrations()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Grid RowDefinitions="Auto,*" RowSpacing="12" Padding="16" MinHeight="240" MaxWidth="720">
                <TextBlock x:Name="TitleText" Grid.Row="1" Grid.ColumnSpan="2" Text="Production-like row" />
              </Grid>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "__element1.RowDefinitions = \"Auto,*\"");
        StringAssert.Contains(result.GeneratedSource, "__element1.RowSpacing = 12");
        StringAssert.Contains(result.GeneratedSource, "__element1.Padding = \"16\"");
        StringAssert.Contains(result.GeneratedSource, "__element1.MinHeight = 240");
        StringAssert.Contains(result.GeneratedSource, "__element1.MaxWidth = 720");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Controls.Grid.SetRow(__element2, 1)");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.Controls.Grid.SetColumnSpan(__element2, 2)");
    }

    [TestMethod]
    public void CompileTextGeneratesBorderAndScrollViewerProductionLayoutProperties()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
                <Border Padding="20,12" BorderBrush="{ThemeResource CardStrokeBrush}" BorderThickness="1" MaxWidth="640">
                  <ProgressRing Width="24" Height="24" IsActive="True" />
                </Border>
              </ScrollViewer>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "__element1.HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Disabled");
        StringAssert.Contains(result.GeneratedSource, "__element2.Padding = \"20,12\"");
        StringAssert.Contains(result.GeneratedSource, "__element2.BorderBrush = Microsoft.UI.Xaml.ResourceOperations.Resolve(__resources, \"CardStrokeBrush\", \"BorderBrush\")");
        StringAssert.Contains(result.GeneratedSource, "__element2.BorderThickness = \"1\"");
        StringAssert.Contains(result.GeneratedSource, "__element2.MaxWidth = 640");
        StringAssert.Contains(result.GeneratedSource, "__element3.Width = 24");
        StringAssert.Contains(result.GeneratedSource, "__element3.Height = 24");
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
    public void CompileTextAcceptsStandaloneResourceDictionaryWithoutClass()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="CardStrokeBrush" Color="#FFE5E5E5" />
              <Style x:Key="CardTitleStyle" TargetType="TextBlock">
                <Setter Property="TextWrapping" Value="Wrap" />
              </Style>
            </ResourceDictionary>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "Themes/Tokens.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "ResourceDictionary");
        StringAssert.Contains(result.GeneratedSource, "CardStrokeBrush");
        StringAssert.Contains(result.GeneratedSource, "CardTitleStyle");
    }

    [TestMethod]
    public void CompileTextRejectsUnsupportedStandaloneResourceDictionaryChildren()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <DataTemplate x:Key="UnsupportedTemplate" />
            </ResourceDictionary>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "Themes/Components.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML2004", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "DataTemplate");
    }

    [TestMethod]
    public void CompileTextGeneratesPasswordAndMultilineTextFormProperties()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <StackPanel>
                <TextBlock Text="Use a strong password." TextWrapping="Wrap" />
                <PasswordBox x:Name="SecretBox" Password="not-a-real-secret" PlaceholderText="Password" Header="Account password" />
                <TextBox x:Name="NotesBox" TextWrapping="Wrap" AcceptsReturn="True" MinHeight="96" Text="Line one" />
              </StackPanel>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "LoginPanel.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.PasswordBox()");
        StringAssert.Contains(result.GeneratedSource, "__element3.Password = \"not-a-real-secret\"");
        StringAssert.Contains(result.GeneratedSource, "__element3.PlaceholderText = \"Password\"");
        StringAssert.Contains(result.GeneratedSource, "__element3.Header = \"Account password\"");
        StringAssert.Contains(result.GeneratedSource, "__element4.AcceptsReturn = true");
        StringAssert.Contains(result.GeneratedSource, "__element4.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap");
        StringAssert.Contains(result.GeneratedSource, "__element4.MinHeight = 96");
    }

    [TestMethod]
    public void CompileTextGeneratesBoundedListsCommandContentAndStatusProperties()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Grid SizeChanged="OnWorkspaceSizeChanged">
                <InfoBar x:Name="StatusInfo" IsClosable="False" />
                <CommandBar DefaultLabelPosition="Right">
                  <CommandBar.Content>
                    <AutoSuggestBox x:Name="SearchBox" QuerySubmitted="OnQuerySubmitted" TextChanged="OnTextChanged">
                      <AutoSuggestBox.QueryIcon>
                        <SymbolIcon Symbol="Find" />
                      </AutoSuggestBox.QueryIcon>
                    </AutoSuggestBox>
                  </CommandBar.Content>
                </CommandBar>
                <ListView IsItemClickEnabled="True" SelectionMode="Single" SelectionChanged="OnSelectionChanged">
                  <ListView.ItemTemplate>
                    <DataTemplate>
                      <Grid Padding="0,12" RowDefinitions="Auto,4,Auto">
                        <TextBlock FontWeight="SemiBold" Text="{Binding Title}" TextWrapping="Wrap" />
                      </Grid>
                    </DataTemplate>
                  </ListView.ItemTemplate>
                </ListView>
                <ItemsControl>
                  <ItemsControl.ItemTemplate>
                    <DataTemplate>
                      <TextBlock Text="{Binding Name}" />
                    </DataTemplate>
                  </ItemsControl.ItemTemplate>
                </ItemsControl>
              </Grid>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "AdminSurface.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "__element2.IsClosable = false");
        StringAssert.Contains(result.GeneratedSource, "__element3.DefaultLabelPosition = Microsoft.UI.Xaml.Controls.CommandBarDefaultLabelPosition.Right");
        StringAssert.Contains(result.GeneratedSource, "__element3.Content = __element4");
        StringAssert.Contains(result.GeneratedSource, "__element4.QuerySubmitted += OnQuerySubmitted");
        StringAssert.Contains(result.GeneratedSource, "__element4.TextChanged += OnTextChanged");
        StringAssert.Contains(result.GeneratedSource, "__element4.QueryIcon = __element5");
        StringAssert.Contains(result.GeneratedSource, "__element6.IsItemClickEnabled = true");
        StringAssert.Contains(result.GeneratedSource, "__element6.SelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode.Single");
        StringAssert.Contains(result.GeneratedSource, "__element6.SelectionChanged += OnSelectionChanged");
        StringAssert.Contains(result.GeneratedSource, "__element6.ItemTemplate = __element7");
        StringAssert.Contains(result.GeneratedSource, "__element9.FontWeight = \"SemiBold\"");
        StringAssert.Contains(result.GeneratedSource, "__element10.ItemTemplate = __element11");
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
    public void CompileTextReportsPlannedMarkupExtensions()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="{x:Bind Title}" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1007", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "x:Bind");
        StringAssert.Contains(result.Diagnostics[0].Message, "cataloged as planned");
        Assert.IsNotNull(result.Diagnostics[0].Line);
    }

    [TestMethod]
    public void CompileTextReportsUnknownMarkupExtensions()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="{FuturePublicExtension Value}" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1007", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "not present in the WinUI compatibility catalog");
    }

    [TestMethod]
    public void CompileTextAcceptsRecognizedMarkupExtensions()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="{Binding Title}" Foreground="{ThemeResource AccentBrush}" Background="{StaticResource SurfaceBrush}" />
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
    }

    [TestMethod]
    public void CompileTextReportsUnsupportedTemplateConstructs()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ListView>
                <ListView.ItemTemplate>
                  <DataTemplate>
                    <TextBlock Text="{x:Bind Title}" />
                  </DataTemplate>
                </ListView.ItemTemplate>
              </ListView>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "MainWindow.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1007", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "x:Bind");
        StringAssert.Contains(result.Diagnostics[0].Message, "cataloged as planned");
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
    public void CompileTextMergesFrameworkElementResourcesForResourceReferences()
    {
        const string xaml = """
            <Window
                x:Class="Sample.MainWindow"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <NavigationView>
                <NavigationView.Resources>
                  <ResourceDictionary>
                    <ResourceDictionary.ThemeDictionaries>
                      <ResourceDictionary x:Key="Light">
                        <SolidColorBrush x:Key="AccentBrush" Color="#2562D9" />
                      </ResourceDictionary>
                    </ResourceDictionary.ThemeDictionaries>
                    <SolidColorBrush x:Key="AccentBrush" Color="#2562D9" />
                  </ResourceDictionary>
                </NavigationView.Resources>
                <TextBlock Text="Scoped resource" Foreground="{ThemeResource AccentBrush}" />
              </NavigationView>
            </Window>
            """;

        var result = new MacXamlCompiler().CompileText(xaml);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "\"AccentBrush\"");
        StringAssert.Contains(result.GeneratedSource, ".ThemeDictionaries[\"Light\"]");
        StringAssert.Contains(result.GeneratedSource, "ResourceOperations.Resolve(__resources, \"AccentBrush\", \"Foreground\")");
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
