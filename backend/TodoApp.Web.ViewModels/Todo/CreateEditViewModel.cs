using System.ComponentModel.DataAnnotations;

using static TodoApp.GCommon.ModelValidations.Todo;
using static TodoApp.GCommon.ModelsErrorMessages;

namespace TodoApp.Models.Todo;

public class CreateEditViewModel: IValidatableObject
{
    [Required]
    [StringLength(NameMaxLength,
        MinimumLength = NameMinLength,
        ErrorMessage = TodoNameErrorMessage)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(DescriptionMaxLength,
        MinimumLength = DescriptionMinLength,
        ErrorMessage = TodoDescriptionErrorMessage)]
    public string Description { get; set; } = null!;

    [Required]
    public DateOnly DueDate { get; set; }

    public string Priority { get; set; } = null!;

    public string[] Priorities { get; set; } =
    { 
        "High", 
        "Medium",
        "Low"
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOnly.FromDateTime(DateTime.Now) > this.DueDate)
        {
            yield return new ValidationResult("Enter valid date", new[] { nameof(DueDate) });
        }
    }
}