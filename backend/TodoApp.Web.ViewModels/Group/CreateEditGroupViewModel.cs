using System.ComponentModel.DataAnnotations;
using TodoApp.GCommon;

using static TodoApp.GCommon.ModelValidations.Group;
using static TodoApp.GCommon.ModelsErrorMessages;

namespace TodoApp.Models.Group;

public class CreateEditGroupViewModel
{
    [Required]
    [StringLength(NameMaxLength,
        MinimumLength = NameMinLength,
        ErrorMessage = InvalidGroupName)]
    public string GroupName { get; set; } = null!;
}