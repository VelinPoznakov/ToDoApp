using ToDoApp.Maui.ViewModels;

namespace ToDoApp.Maui.Views;

public partial class TodosPage : ContentPage
{
    private readonly TodosViewModel _viewModel;

    public TodosPage(TodosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Auto-load when the page opens
        if (_viewModel.LoadTodosCommand.CanExecute(null))
            _viewModel.LoadTodosCommand.Execute(null);
    }
}
