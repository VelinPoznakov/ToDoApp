using ToDoApp.Maui.Views;

namespace ToDoApp.Maui;

public partial class App : Application
{
    public App(TodosPage todosPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(todosPage);
    }


	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}