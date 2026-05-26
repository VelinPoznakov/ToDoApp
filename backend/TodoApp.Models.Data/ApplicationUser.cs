using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

using static TodoApp.GCommon.ModelValidations.ApplicationUser;

namespace TodoApp.Models.Data
{
    public class ApplicationUser: IdentityUser<Guid>
    {
        [PersonalData]
        [MaxLength(FirstNameMaxLength)]
        public string FirstName { get; set; } = null!;

        [PersonalData]
        [MaxLength(LastNameMaxLength)]
        public string LastName { get; set; } = null!;

        public virtual ICollection<TodoEntity> Todos { get; set; } 
            = new List<TodoEntity>();
    }
}
