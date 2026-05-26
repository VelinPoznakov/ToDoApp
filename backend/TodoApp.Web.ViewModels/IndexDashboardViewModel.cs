using TodoApp.Models.Todo;

namespace TodoApp.Models;

public class IndexDashboardViewModel
{
    public IEnumerable<TodoWithGroupNameViewModel> Todos { get; set; } 
        = new List<TodoWithGroupNameViewModel>();
    public int CountPendingTodos { get; set; }
    public int CountCompletedTodos { get; set; }
    public int CountGroups { get; set; }
}