namespace TodoApp.Models.SupportMessage;

public class AllSupportMessagesViewModel
{
    public int Id { get; set; }
    
    public string Title { get; set; } = null!;

    public string CreatedOn { get; set; } = null!;

    public string IssuedUserFullname { get; set; } = null!;
    
    public string? HandledByFullName { get; set; }
    
    public string? HandledOn { get; set; }
}