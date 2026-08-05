using System.ComponentModel.DataAnnotations;

using static TodoApp.GCommon.ModelValidations.SupportMessage;
using static TodoApp.GCommon.ModelsErrorMessages;

namespace TodoApp.Web.ViewModels.SupportMessage;

public class CreateSupportMessageViewModel
{
    [Required]
    [StringLength(TitleMaxLength,
        MinimumLength = TitleMinLength,
        ErrorMessage = SupportMessageTitleErrorMessage)]
    public string Title { get; set; } = null!;

    [Required]
    [StringLength(TitleMaxLength,
        MinimumLength = TitleMinLength,
        ErrorMessage = SupportMessageDescriptionErrorMessage)]
    public string Description { get; set; } = null!;
}