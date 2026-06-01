using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace InteractionBindingApp.MacTest;

public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellState();
        DetailsFrame.Navigate(typeof(DetailPage), new DetailPageContext("Details ready"));
        BindingOperations.RefreshTree(this);
    }
}

public sealed class ShellState : INotifyPropertyChanged
{
    private string title = "Initial title";
    private string buttonLabel = "Refresh";
    private string searchText = "Open tasks";

    public ShellState()
    {
        RefreshCommand = new RelayCommand(_ => Refresh());
        Tasks.Add("Review intake queue");
        Tasks.Add("Confirm reviewer assignment");
        Tasks.Add("Publish daily summary");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => title;
        private set => SetProperty(ref title, value, nameof(Title));
    }

    public string ButtonLabel
    {
        get => buttonLabel;
        private set => SetProperty(ref buttonLabel, value, nameof(ButtonLabel));
    }

    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value, nameof(SearchText));
    }

    public ObservableCollection<string> Tasks { get; } = new();

    public ICommand RefreshCommand { get; }

    private void Refresh()
    {
        Title = "Updated title";
        ButtonLabel = "Done";
        Tasks.Add("Archive completed task");
    }

    private void SetProperty(ref string field, string value, string propertyName)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

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

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> execute;

    public RelayCommand(Action<object?> execute)
    {
        this.execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        execute(parameter);
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
