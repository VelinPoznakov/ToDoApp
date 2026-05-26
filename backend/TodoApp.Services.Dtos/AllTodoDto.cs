namespace TodoApp.Services.Dtos;

public class AllTodoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string DueDate { get; set; } = null!;
    public string GroupName { get; set; } = null!;



}