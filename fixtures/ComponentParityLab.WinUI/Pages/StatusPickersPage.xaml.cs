using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class StatusPickersPage : Page
{
    public StatusPickersPage()
    {
        InitializeComponent();
        NativeControlSamples.PopulateStatusAndPickers(
            DiagnosticInfoBadge,
            DiagnosticPersonPicture,
            DiagnosticColorPicker,
            DiagnosticCalendarDatePicker,
            DiagnosticCalendarView,
            DiagnosticDatePicker,
            DiagnosticTimePicker);
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("loading", StringComparison.OrdinalIgnoreCase))
        {
            StatusInfoBar.Title = "Loading";
            StatusInfoBar.Message = "The public lab status is loading.";
            StatusInfoBar.Severity = InfoBarSeverity.Informational;
            CompletionProgressBar.Value = 35;
            LoadingProgressRing.IsActive = true;
            return;
        }

        if (scenarioName.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            StatusInfoBar.Title = "Error";
            StatusInfoBar.Message = "The public lab status has a validation error.";
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            CompletionProgressBar.Value = 15;
            LoadingProgressRing.IsActive = false;
            return;
        }

        if (scenarioName.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            StatusInfoBar.Title = "Disabled";
            StatusInfoBar.Message = "The public lab action is disabled.";
            StatusInfoBar.Severity = InfoBarSeverity.Warning;
            CompleteStatusButton.IsEnabled = false;
            LoadingProgressRing.IsActive = false;
            return;
        }

        if (scenarioName.Contains("status-pickers", StringComparison.OrdinalIgnoreCase))
        {
            StatusInfoBar.Title = "Complete";
            StatusInfoBar.Message = "The public lab status completed.";
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            CompletionProgressBar.Value = 100;
            LoadingProgressRing.IsActive = false;
        }
    }

    private void OnCompleteClicked(object sender, RoutedEventArgs args)
    {
        StatusInfoBar.Title = "Complete";
        StatusInfoBar.Message = "The public lab status completed.";
        StatusInfoBar.Severity = InfoBarSeverity.Success;
        CompletionProgressBar.Value = 100;
        LoadingProgressRing.IsActive = false;
    }
}
