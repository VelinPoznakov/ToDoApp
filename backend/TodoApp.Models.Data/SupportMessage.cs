using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using static TodoApp.GCommon.ModelValidations.SupportMessage;

namespace TodoApp.Models.Data;

public class SupportMessage
{
    [Comment("Support message primary key")]
    [Key]
    public int Id { get; set; }

    [Comment("Support message title")]
    [Required]
    [MaxLength(TitleMaxLength)]
    public string Title { get; set; } = null!;

    [Comment("Support message description")]
    [Required]
    [MaxLength(DescriptionMaxLength)]
    public string Description { get; set; } = null!;
    
    [Comment("Support message created on date")]
    [Required]
    public DateTime CreatedOn { get; set; }
    
    [Comment("Support message handled on date")]
    public DateTime HandledOn { get; set; }

    [Comment("Support message completed on date")]
    [Required]
    public bool IsHandled { get; set; } = false;
    
    [Comment("Support message user fk")]
    [Required]
    [ForeignKey(nameof(ApplicationUser))]
    public Guid ApplicationUserId { get; set; }
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;
}