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
}
