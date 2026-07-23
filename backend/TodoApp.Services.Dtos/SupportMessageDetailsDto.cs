namespace TodoApp.Services.Dtos;

public class SupportMessageDetailsDto
{
    public int Id { get; set; }
    
    public string Title { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public DateTime CreatedOn { get; set; }
    
    public bool IsHandled { get; set; }
    
    public string IssuedUserFullName { get; set; } = null!;
    
    public DateTime? HandledOn { get; set; }
    
    public string? HandledByUserFullName { get; set; }

}