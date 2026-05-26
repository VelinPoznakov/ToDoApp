using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static TodoApp.GCommon.ModelValidations.Todo;
using static TodoApp.GCommon.ModelValidations.DataTypes;

namespace TodoApp.Models.Data
{
    public class TodoEntity
    {
        [Comment("Todo entity primary key")]
        [Key]
        public Guid Id { get; set; }

        [Comment("Todo name")]
        [Required]
        [MaxLength(NameMaxLength)]
        [Unicode(true)]
        public string Name { get; set; } = null!;

        [Comment("Todo description")]
        [Required]
        [MaxLength(DescriptionMaxLength)]
        [Unicode(true)]
        public string Description { get; set; } = null!;

        [Comment("Todo due date")]
        [Required]
        [Column(TypeName = DateTimeConstant)]
        public DateTime DueDate { get; set; }

        [Comment("Todo completion status")]
        [Required]
        [Column(TypeName = BoolDataTypeConstant)]
        public bool IsCompleted { get; set; } = false;

        [Comment("Todo creation date")]
        [Required]
        [Column(TypeName = DateTimeConstant)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Comment("Todo last update date, Nullable")]
        [Column(TypeName = DateTimeConstant)]
        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
