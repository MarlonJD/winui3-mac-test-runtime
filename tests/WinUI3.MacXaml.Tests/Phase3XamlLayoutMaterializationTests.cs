using WinUI3.MacXaml;

namespace WinUI3.MacXaml.Tests;

[TestClass]
public sealed class Phase3XamlLayoutMaterializationTests
{
    [TestMethod]
    public void Phase3ContentPresenterMaterializesAsSingleContentSlot()
    {
        const string xaml = """
            <Page
                x:Class="Sample.LoginPage"
                xmlns="using:Microsoft.UI.Xaml"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Grid>
                <ContentPresenter x:Name="LoginPresenter">
                  <StackPanel>
                    <TextBlock Text="Sign in" />
                  </StackPanel>
                </ContentPresenter>
              </Grid>
            </Page>
            """;

        var result = new MacXamlCompiler().CompileText(xaml, "LoginPage.xaml");

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(0, result.Diagnostics);
        StringAssert.Contains(result.GeneratedSource, "public Microsoft.UI.Xaml.Controls.ContentPresenter LoginPresenter");
        StringAssert.Contains(result.GeneratedSource, "new Microsoft.UI.Xaml.Controls.ContentPresenter()");
        StringAssert.Contains(result.GeneratedSource, "__element2.Content = __element3");
    }
}
