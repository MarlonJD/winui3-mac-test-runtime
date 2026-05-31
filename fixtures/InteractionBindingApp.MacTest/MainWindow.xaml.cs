using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace InteractionBindingApp.MacTest;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellState("Initial title", "Refresh");
        DetailsFrame.Navigate(typeof(DetailPage), new DetailPageContext("Details ready"));
        BindingOperations.RefreshTree(this);
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        DataContext = new ShellState("Updated title", "Done");
        BindingOperations.RefreshTree(this);
    }
}

public sealed record ShellState(string Title, string ButtonLabel);

public sealed record DetailPageContext(string Message);

public sealed class DetailPage : Page
{
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs args)
    {
        var context = (DetailPageContext?)args.Parameter;
        Content = new TextBlock
        {
            Name = "DetailText",
            Text = context?.Message
        };
    }
}
