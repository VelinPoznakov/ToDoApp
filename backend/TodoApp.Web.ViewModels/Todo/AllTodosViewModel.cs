namespace TodoApp.Web.ViewModels.Todo;

public class AllTodosViewModel
{
    public Guid GroupId { get; set; }

    public IEnumerable<TodoViewModel> Todos { get; set; } = new List<TodoViewModel>();
    public string GroupName { get; set; } = null!;
}
