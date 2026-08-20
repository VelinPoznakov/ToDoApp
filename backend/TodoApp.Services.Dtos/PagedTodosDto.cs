namespace TodoApp.Services.Dtos;

public class PagedTodosDto
{
    public IEnumerable<AllTodoDto> Todos { get; set; }
        = new List<AllTodoDto>();

    public string GroupName { get; set; } = null!;
    
    public int PageNumber { get; set; }
}