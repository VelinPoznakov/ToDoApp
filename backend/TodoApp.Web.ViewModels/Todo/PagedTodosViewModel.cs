namespace TodoApp.Web.ViewModels.Todo;

public class PagedTodosViewModel
{
    public IEnumerable<TodoViewModel> Todos { get; set; } = new List<TodoViewModel>();

    public string GroupName { get; set; } = null!;
    
    public Guid GroupId { get; set; }
    
    public int PageNumber { get; set; }
}