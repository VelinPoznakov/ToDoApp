using TodoApp.Models.Comment;

namespace TodoApp.Models.Todo;

public class TodoDetailsViewmodel
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string DueDate { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public string CreatedOn { get; set; } = null!;

    public ICollection<CommentViewModel> Comments { get; set; } 
        = new List<CommentViewModel>();

}