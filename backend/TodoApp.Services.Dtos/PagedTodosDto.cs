namespace TodoApp.Services.Dtos;

public class PagedTodosDto<T>
{
    public IEnumerable<T> Todos { get; set; }
        = new List<T>();

    public string GroupName { get; set; } = null!;
    
    public int PageNumber { get; set; }
}