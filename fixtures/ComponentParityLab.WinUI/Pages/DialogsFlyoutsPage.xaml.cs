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
#if !WINDOWS
        PopulateManagedPopupFixtures();
#endif
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("open-popup", StringComparison.OrdinalIgnoreCase))
        {
            DialogsFlyoutsStateText.Text = "Open popup targets visible";
#if !WINDOWS
            SetPopupOpenState(DiagnosticContentDialog, true);
            SetPopupOpenState(DiagnosticFlyout, true);
            SetPopupOpenState(DiagnosticTeachingTip, true);
            SetPopupOpenState(DiagnosticToolTip, true);
#endif
            return;
        }

        if (scenarioName.Contains("dialogs-flyouts", StringComparison.OrdinalIgnoreCase))
        {
            DialogsFlyoutsStateText.Text = "Dialog and flyout targets ready";
        }
    }

#if !WINDOWS
    private void PopulateManagedPopupFixtures()
    {
        DiagnosticContentDialog.Content = new Button
        {
            Content = "Open dialog",
            Flyout = new ContentDialog
            {
                Title = "Public dialog",
                Content = "Review public fixture state",
                PrimaryButtonText = "OK"
            }
        };
        DiagnosticFlyout.Content = new Button
        {
            Content = "Open flyout",
            Flyout = new Flyout
            {
                Content = new TextBlock { Text = "Open flyout content" }
            }
        };
        DiagnosticTeachingTip.Content = new Button
        {
            Content = "Open teaching tip",
            Flyout = new TeachingTip
            {
                Title = "Teaching tip",
                Subtitle = "Managed popup sample"
            }
        };
        DiagnosticToolTip.Content = new Button
        {
            Content = "Hover tooltip target",
            Flyout = new ToolTip
            {
                Content = "Managed tooltip content"
            }
        };
        DiagnosticToolTipServiceSetToolTip.Content = new Button
        {
            Content = "Tooltip service target",
            Flyout = new ToolTip
            {
                Content = "Managed ToolTipService sample"
            }
        };
    }

    private static void SetPopupOpenState(ContentControl host, bool isOpen)
    {
        switch (host.Content)
        {
            case Button { Flyout: ContentDialog dialog }:
                if (isOpen)
                {
                    dialog.Show();
                }
                else
                {
                    dialog.Hide("dismissed");
                }

                break;
            case Button { Flyout: Flyout flyout }:
                flyout.IsOpen = isOpen;
                break;
            case Button { Flyout: TeachingTip teachingTip }:
                teachingTip.IsOpen = isOpen;
                break;
            case Button { Flyout: ToolTip toolTip }:
                toolTip.IsOpen = isOpen;
                break;
        }
    }
#endif
}
