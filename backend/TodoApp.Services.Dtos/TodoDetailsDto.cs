namespace TodoApp.Services.Dtos;

public class TodoDetailsDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string DueDate { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public string CreatedOn { get; set; } = null!;

    public Guid GroupId { get; set; }

    public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
}