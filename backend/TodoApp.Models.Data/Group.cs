using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using static TodoApp.GCommon.ModelValidations.Group;
using static TodoApp.GCommon.ModelValidations.DataTypes;

namespace TodoApp.Models.Data;

public class Group
{
    [Comment("Unique identifier for the group")]
    [Key]
    public Guid Id { get; set; }

    [Comment("Name of the group")]
    [Required]
    [MaxLength(NameMaxLength)]
    [Unicode(true)]
    public string Name { get; set; } = null!;

    public virtual ICollection<TodoEntity> Todos { get; set; } 
        = new List<TodoEntity>();

    [Comment("Identifier of the user who owns the group")]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    [Comment("Timestamp when the group was created")]
    [Required]
    [Column(TypeName = DateTimeConstant)]
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    [Comment("Timestamp when the group was last updated, Nullable")]
    [Column(TypeName = DateTimeConstant)]
    public DateTime? UpdatedOn { get; set; }
}