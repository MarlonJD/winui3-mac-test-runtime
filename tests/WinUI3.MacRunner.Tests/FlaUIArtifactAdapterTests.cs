using System.Text.Json;
using WinUI3.MacRunner.Automation;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class FlaUIArtifactAdapterTests
{
    [TestMethod]
    public void FlaUIArtifactAdapterLooksUpElementByAutomationId()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var element = adapter.FindByAutomationId("shell-nav-messages");

        Assert.IsNotNull(element);
        Assert.AreEqual("shell-nav-messages", element.AutomationId);
        Assert.AreEqual("MessagesNavigationItem", element.Name);
        Assert.AreEqual("navigation-item", element.Role);
    }

    [TestMethod]
    public void FlaUIArtifactAdapterLooksUpElementByName()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var element = adapter.FindByName("LogoutButton");

        Assert.IsNotNull(element);
        Assert.AreEqual("shell-logout", element.AutomationId);
        Assert.AreEqual("LogoutButton", element.Name);
        Assert.AreEqual("Log out", element.Label);
        Assert.AreEqual("Signs out the current user", element.HelpText);
    }

    [TestMethod]
    public void FlaUIArtifactAdapterMapsControlTypesFromAccessibilityAndTree()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        Assert.AreEqual(ArtifactControlType.Button, adapter.FindByAutomationId("shell-logout")!.ControlType);
        Assert.AreEqual(ArtifactControlType.CheckBox, adapter.FindByAutomationId("remember")!.ControlType);
        Assert.AreEqual(ArtifactControlType.Edit, adapter.FindByAutomationId("search")!.ControlType);
        Assert.AreEqual(ArtifactControlType.ListItem, adapter.FindByAutomationId("shell-nav-messages")!.ControlType);

        // FrameworkType keeps the original WinUI control type for transparency.
        Assert.AreEqual("Microsoft.UI.Xaml.Controls.Button", adapter.FindByAutomationId("shell-logout")!.FrameworkType);
    }

    [TestMethod]
    public void FlaUIArtifactAdapterExposesSelectedCheckedEnabledFocusedValueState()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var navigationItem = adapter.FindByAutomationId("shell-nav-messages")!;
        Assert.AreEqual(true, navigationItem.IsSelected);
        Assert.AreEqual(true, navigationItem.IsEnabled);
        Assert.IsFalse(navigationItem.HasKeyboardFocus);

        var checkBox = adapter.FindByAutomationId("remember")!;
        Assert.AreEqual(true, checkBox.IsChecked);
        Assert.AreEqual(ArtifactToggleState.On, checkBox.ToggleState);

        var searchBox = adapter.FindByAutomationId("search")!;
        Assert.IsTrue(searchBox.HasKeyboardFocus);
        Assert.AreEqual("hello", searchBox.Value);
        Assert.AreEqual(true, searchBox.IsKeyboardFocusable);

        var expander = adapter.FindByAutomationId("details")!;
        Assert.AreEqual(true, expander.IsExpanded);
        Assert.AreEqual(ArtifactExpandCollapseState.Expanded, expander.ExpandCollapseState);
        Assert.AreEqual(false, expander.IsEnabled);
    }

    [TestMethod]
    public void FlaUIArtifactAdapterExposesBoundingRectangleWhenLayoutPresent()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var bounds = adapter.FindByAutomationId("shell-nav-messages")!.BoundingRectangle;
        Assert.IsNotNull(bounds);
        Assert.AreEqual(12, bounds.X);
        Assert.AreEqual(48, bounds.Y);
        Assert.AreEqual(224, bounds.Width);
        Assert.AreEqual(40, bounds.Height);

        // No layout block in the artifacts => no bounding rectangle is fabricated.
        Assert.IsNull(adapter.FindByAutomationId("remember")!.BoundingRectangle);
    }

    [TestMethod]
    public void FlaUIArtifactAdapterLooksUpActionResultBySelector()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var passed = adapter.FindActionBySelector("automationId=shell-nav-messages");
        Assert.IsNotNull(passed);
        Assert.AreEqual("passed", passed.Status);
        Assert.AreEqual("assertAccessibilityState", passed.Type);
        Assert.AreEqual("automationId", passed.SelectorKind);

        var failed = adapter.FindActionBySelector("ContentFrame");
        Assert.IsNotNull(failed);
        Assert.AreEqual("failed", failed.Status);
        Assert.AreEqual("messages", failed.Expected);
        Assert.AreEqual("home", failed.Actual);

        Assert.IsNull(adapter.FindActionBySelector("automationId=does-not-exist"));
    }

    [TestMethod]
    public void FlaUIArtifactAdapterCompatibilityReportListsSupportedAndUnsupportedConcepts()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var report = adapter.BuildCompatibilityReport();

        Assert.IsFalse(report.IsNativeUiaProvider);
        var supported = report.SupportedConcepts.Select(concept => concept.Name).ToArray();
        var unsupported = report.UnsupportedConcepts.Select(concept => concept.Name).ToArray();

        CollectionAssert.Contains(supported, "FindByAutomationId");
        CollectionAssert.Contains(supported, "FindByName");
        CollectionAssert.Contains(supported, "ControlTypeMapping");
        CollectionAssert.Contains(supported, "SelectionItemPattern.IsSelected");
        CollectionAssert.Contains(supported, "TogglePattern.ToggleState");
        CollectionAssert.Contains(supported, "ExpandCollapsePattern.ExpandCollapseState");
        CollectionAssert.Contains(supported, "ValuePattern.Value");
        CollectionAssert.Contains(supported, "BoundingRectangle");
        CollectionAssert.Contains(supported, "ActionResultLookup");

        CollectionAssert.Contains(unsupported, "NativeUiaProvider");
        CollectionAssert.Contains(unsupported, "PatternMethodInvocation");
        CollectionAssert.Contains(unsupported, "RealPointerKeyboardInput");
        CollectionAssert.Contains(unsupported, "UiaEventSubscriptions");

        // The adapter must not pretend that unmodified FlaUI tests already run on macOS.
        CollectionAssert.Contains(unsupported, "UnchangedFlaUITestExecution");
    }

    [TestMethod]
    public void FlaUIArtifactAdapterEmitsCompatibilityAndParityReportArtifacts()
    {
        var directory = ArtifactAdapterFixture.Write();
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(directory);
        var outputDirectory = Path.Combine(directory, "adapter-report");

        var paths = adapter.WriteReports(outputDirectory);

        Assert.IsTrue(File.Exists(paths.CompatibilityReportPath));
        Assert.IsTrue(File.Exists(paths.ParityReportPath));

        using var compatibility = JsonDocument.Parse(File.ReadAllText(paths.CompatibilityReportPath));
        Assert.IsFalse(compatibility.RootElement.GetProperty("isNativeUiaProvider").GetBoolean());
        Assert.IsTrue(compatibility.RootElement.GetProperty("supportedConcepts").GetArrayLength() > 0);
        Assert.IsTrue(compatibility.RootElement.GetProperty("unsupportedConcepts").GetArrayLength() > 0);

        using var parity = JsonDocument.Parse(File.ReadAllText(paths.ParityReportPath));
        Assert.IsFalse(parity.RootElement.GetProperty("windowsReferenceRun").GetBoolean());
        Assert.AreEqual(3, parity.RootElement.GetProperty("actions").GetArrayLength());
    }
}
