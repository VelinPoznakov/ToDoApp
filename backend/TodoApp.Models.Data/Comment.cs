using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using static TodoApp.GCommon.ModelValidations.Comment;
using static TodoApp.GCommon.ModelValidations.DataTypes;

namespace TodoApp.Models.Data;

public class Comment
{
    [Comment("Unique identifier for the comment")]
    [Key]
    public int Id { get; set; }

    [Comment("Content of the comment")]
    [Required]
    [MaxLength(ContentMaxLength)]
    public string Content { get; set; } = null!;

    [Comment("Identifier for the todo to which the comment belongs")]
    [ForeignKey(nameof(TodoEntity))]
    public Guid TodoId { get; set; }
    public virtual TodoEntity TodoEntity { get; set; } = null!;

    [Comment("Timestamp when the comment was created")]
    [Required]
    [Column(TypeName = DateTimeConstant)]
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    [Comment("Timestamp when the comment was last updated")]
    [Column(TypeName = DateTimeConstant)]
    public DateTime? UpdatedOn { get; set; }

}