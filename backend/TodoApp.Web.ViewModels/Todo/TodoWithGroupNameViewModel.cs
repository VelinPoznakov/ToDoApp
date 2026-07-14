using TodoApp.Web.ViewModels.Todo;

namespace TodoApp.Models.Todo;

public class TodoWithGroupNameViewModel: TodoViewModel
{
    public string GroupName { get; set; } = null!;

    public Guid GroupId { get; set; }
}