namespace TodoApp.Models.Data;

public class AdminUser: ApplicationUser
{
    public virtual ICollection<SupportMessage> HandledSupportMessages { get; set; }
        = new List<SupportMessage>();
}