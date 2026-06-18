using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TodoApp.Models.Data.Enums;
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

        [Comment("Todo priority")] [Required] public Priority Priority { get; set; }

        [Comment("Todo status")] [Required] public Status Status { get; set; } = Status.Pending;

        [Comment("Todo due date")] [Required] public DateOnly DueDate { get; set; }

        [Comment("Todo creation date")]
        [Required]
        [Column(TypeName = DateTimeConstant)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Comment("Todo last update date, Nullable")]
        [Column(TypeName = DateTimeConstant)]
        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(User))] public Guid UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(Group))] public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } = null!;

        public virtual ICollection<Comment> Comments { get; set; }
            = new List<Comment>();
    }
}
