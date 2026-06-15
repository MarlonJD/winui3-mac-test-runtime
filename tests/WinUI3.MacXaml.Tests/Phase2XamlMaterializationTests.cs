using WinUI3.MacXaml;

namespace WinUI3.MacXaml.Tests;

[TestClass]
public sealed class Phase2XamlMaterializationTests
{
    [TestMethod]
    public void Phase2MaterializationContractNamesPortableMvpElements()
    {
        CollectionAssert.AreEquivalent(
            new[]
            {
                "Window",
                "Page",
                "Grid",
                "StackPanel",
                "Border",
                "TextBlock",
                "Button",
                "TextBox",
                "CheckBox",
                "RadioButton",
                "Frame",
            },
            PortableXamlMaterialization.Phase2ElementNames.ToArray());
    }

    [TestMethod]
    public void Phase2LoginPageMaterializesSupportedControlsAndResources()
    {
        const string xaml = """
            <Page
                x:Class="Sample.LoginPage"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Page.Resources>
                <ResourceDictionary>
                  <SolidColorBrush x:Key="PanelBrush" Color="#FFFFFFFF" />
                  <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key="Dark">
                      <SolidColorBrush x:Key="PanelBrush" Color="#FF202020" />
                    </ResourceDictionary>
                  </ResourceDictionary.ThemeDictionaries>
                </ResourceDictionary>
              </Page.Resources>
              <Grid>
                <Border Background="{ThemeResource PanelBrush}">
                  <StackPanel>
                    <TextBlock Text="Sign in" />
                    <TextBox x:Name="UsernameBox" Text="{Binding UserName, Mode=TwoWay}" />
                    <CheckBox x:Name="RememberBox" Content="Remember me" IsChecked="True" />
                    <RadioButton x:Name="StaffRole" Content="Staff" GroupName="Role" />
                    <Button x:Name="SubmitButton" Content="{StaticResource MissingSubmitText}" />
                    <Frame x:Name="ShellFrame" />
                  </StackPanel>
                </Border>
              </Grid>
            </Page>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "LoginPage.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "public sealed partial class LoginPage : Microsoft.UI.Xaml.Controls.Page");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.Grid()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.Border()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.StackPanel()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.TextBlock()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.TextBox()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.CheckBox()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.RadioButton()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.Button()");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.Frame()");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.ResourceOperations.Resolve(__resources, \"PanelBrush\", \"Background\")");
        StringAssert.Contains(result.GeneratedSource, "Microsoft.UI.Xaml.ResourceOperations.Resolve(__resources, \"MissingSubmitText\", \"Content\")");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Data.Binding(\"UserName\", Microsoft.UI.Xaml.Data.BindingMode.TwoWay)");
    }

    [TestMethod]
    public void Phase2UnsupportedMarkupProducesCompatibilityDiagnostic()
    {
        const string xaml = """
            <Page
                x:Class="Sample.LoginPage"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="{x:Bind UserName}" />
            </Page>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "LoginPage.xaml");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("XAML1007", result.Diagnostics[0].Code);
        StringAssert.Contains(result.Diagnostics[0].Message, "x:Bind");
        StringAssert.Contains(result.Diagnostics[0].Message, PortableXamlMaterialization.Phase2Name);
    }
}
