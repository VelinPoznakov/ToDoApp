namespace TodoApp.Services.Dtos;

public class AllSupportMessagesDto
{
    public int Id { get; set; }
    
    public string Title { get; set; } = null!;
    
    public DateTime CreatedOn { get; set; }

    public string IssuedUserFullname { get; set; } = null!;
    
    public string? HandledByFullName { get; set; }
    
    public DateTime? HandledOn { get; set; }
}