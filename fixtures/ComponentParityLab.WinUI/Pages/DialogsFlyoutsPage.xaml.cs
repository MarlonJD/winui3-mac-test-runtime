using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class DialogsFlyoutsPage : Page
{
    public DialogsFlyoutsPage()
    {
        InitializeComponent();
        NativeControlSamples.PopulateDialogsAndFlyouts(
            DiagnosticContentDialog,
            DiagnosticFlyout,
            DiagnosticTeachingTip,
            DiagnosticToolTip,
            DiagnosticToolTipServiceSetToolTip);
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("open-popup", StringComparison.OrdinalIgnoreCase))
        {
            DialogsFlyoutsStateText.Text = "Open popup targets visible";
            return;
        }

        if (scenarioName.Contains("dialogs-flyouts", StringComparison.OrdinalIgnoreCase))
        {
            DialogsFlyoutsStateText.Text = "Dialog and flyout targets ready";
        }
    }
}
