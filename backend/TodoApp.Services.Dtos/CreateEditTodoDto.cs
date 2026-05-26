namespace TodoApp.Services.Dtos;

public class CreateEditTodoDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string DueDate { get; set; } = null!;
    public Guid GroupId { get; set; }
}