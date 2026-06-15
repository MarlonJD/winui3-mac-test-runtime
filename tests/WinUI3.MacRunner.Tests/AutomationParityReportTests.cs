using WinUI3.MacRunner.Automation;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class AutomationParityReportTests
{
    [TestMethod]
    public void AutomationParityReportSummarizesMacActionsWithoutWindowsReference()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var report = adapter.BuildParityReport();

        Assert.IsFalse(report.WindowsReferenceRun);
        Assert.HasCount(3, report.Actions);
        Assert.AreEqual(2, report.PassedOnMac);
        Assert.AreEqual(1, report.FailedOnMac);
        Assert.AreEqual(0, report.SkippedOnMac);
    }

    [TestMethod]
    public void AutomationParityReportMapsEachActionStatusForMacAndWindows()
    {
        var adapter = FlaUIArtifactAdapter.LoadFromDirectory(ArtifactAdapterFixture.Write());

        var report = adapter.BuildParityReport();

        var passed = report.Actions[0];
        Assert.AreEqual(0, passed.Index);
        Assert.AreEqual("assertAccessibilityState", passed.Type);
        Assert.AreEqual(AutomationParityStatus.PassedOnMac, passed.MacStatus);
        Assert.AreEqual(AutomationParityStatus.NotRunOnWindows, passed.WindowsReferenceStatus);

        var failed = report.Actions[2];
        Assert.AreEqual("assertProperty", failed.Type);
        Assert.AreEqual(AutomationParityStatus.FailedOnMac, failed.MacStatus);
        Assert.AreEqual(AutomationParityStatus.NotRunOnWindows, failed.WindowsReferenceStatus);
    }
}
