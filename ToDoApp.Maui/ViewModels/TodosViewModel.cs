using System.Collections.ObjectModel;
using System.Windows.Input;
using ToDoApp.Maui.Models.Response;
using ToDoApp.Maui.Services.Interfaces;

namespace ToDoApp.Maui.ViewModels;

public class TodosViewModel
{
    private readonly ITodoApiService _todoApiService;

    public ObservableCollection<TodoResponseDto> Todos { get; } = new();

    public ICommand LoadTodosCommand { get; }

    public TodosViewModel(ITodoApiService todoApiService)
    {
        _todoApiService = todoApiService;

        LoadTodosCommand = new Command(async () => await LoadTodosAsync());
    }

    private async Task LoadTodosAsync()
    {
        var todos = await _todoApiService.GetAllTodosAsync();

        Todos.Clear();
        foreach (var todo in todos)
        {
            Todos.Add(todo);
        }
    }
}
