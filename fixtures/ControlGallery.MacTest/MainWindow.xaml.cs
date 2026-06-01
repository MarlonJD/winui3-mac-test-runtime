using Microsoft.UI.Xaml;

namespace ControlGallery.MacTest;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        StatusComboBox.Items.Add("Open");
        StatusComboBox.Items.Add("In review");
        StatusComboBox.Items.Add("Closed");
        StatusComboBox.SelectedIndex = 1;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs args)
    {
        StatusInfo.Title = "Saved";
        StatusInfo.Message = "The public control gallery command ran.";
    }
}
