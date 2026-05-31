using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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

    [TestMethod]
    public void BindingOperationsRefreshesTreeAndReportsFailures()
    {
        var textBlock = new TextBlock { Name = "TitleText" };
        BindingOperations.SetBinding(textBlock, nameof(TextBlock.Text), new Binding("Title"));

        var root = new StackPanel { DataContext = new { Title = "Bound title" } };
        root.Children.Add(textBlock);

        BindingOperations.RefreshTree(root);

        Assert.AreEqual("Bound title", textBlock.Text);
        Assert.HasCount(0, BindingOperations.CurrentFailures);

        BindingOperations.SetBinding(textBlock, nameof(TextBlock.Text), new Binding("Missing"));
        BindingOperations.RefreshTree(root);

        Assert.HasCount(1, BindingOperations.CurrentFailures);
        Assert.AreEqual("TitleText", BindingOperations.CurrentFailures[0].ElementName);
    }

    [TestMethod]
    public void InteractionScriptClicksButtonsAndRefreshesBindings()
    {
        var window = new Window();
        var button = new Button { Name = "RefreshButton" };
        var title = new TextBlock { Name = "TitleText" };
        var root = new StackPanel { DataContext = new MutableState("Before") };
        root.Children.Add(title);
        root.Children.Add(button);
        window.Content = root;
        BindingOperations.SetBinding(title, nameof(TextBlock.Text), new Binding("Title"));
        button.Click += (_, _) => root.DataContext = new MutableState("After");

        var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>()))
            .Run(window, new InteractionScript(new[] { new InteractionAction("click", "RefreshButton", null, null, null, null) }));

        Assert.HasCount(1, report.Steps);
        Assert.AreEqual("passed", report.Steps[0].Status);
        Assert.AreEqual("After", title.Text);
    }

    private sealed record MutableState(string Title);
}
