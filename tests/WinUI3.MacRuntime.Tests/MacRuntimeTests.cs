using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class MacRuntimeTests
{
    [TestMethod]
    public void DoctorMarksWineAsOptional()
    {
        var report = MacDoctor.Check();

        Assert.IsFalse(report.PrimaryPathRequiresWine);
        Assert.IsFalse(report.Wine.Required);
        Assert.AreEqual("optional", report.Wine.Status);
    }

    [TestMethod]
    public void TreeBuilderExportsFacadeBackedTree()
    {
        var root = new StackPanel { Name = "RootStack" };
        root.Children.Add(new TextBlock { Name = "GreetingText", Text = "Hello" });
        root.Children.Add(new Button { Name = "PrimaryButton", Content = "Continue" });
        var window = new Window { Title = "Tiny", Content = root };
        window.Activate();

        var tree = UiTreeBuilder.Build(window);

        Assert.AreEqual("Microsoft.UI.Xaml.Window", tree.Root.Type);
        Assert.AreEqual("Tiny", tree.Root.Properties["title"]);
        Assert.HasCount(1, tree.Root.Children);
        var stack = tree.Root.Children[0];
        Assert.AreEqual("RootStack", stack.Name);
        Assert.HasCount(2, stack.Children);
        Assert.AreEqual("GreetingText", stack.Children[0].Name);
        Assert.AreEqual("Hello", stack.Children[0].Properties["text"]);
        Assert.AreEqual("PrimaryButton", stack.Children[1].Name);
        Assert.AreEqual("Continue", stack.Children[1].Properties["content"]);
    }
}
