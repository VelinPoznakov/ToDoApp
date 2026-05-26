using System.ComponentModel.DataAnnotations;

using static TodoApp.GCommon.ModelValidations.ApplicationUser;
using static TodoApp.GCommon.ModelsErrorMessages;

namespace TodoApp.Web.ViewModels.User
{
    public class RegisterViewModel
    {
        [Required]
        [MaxLength(FirstNameMaxLength, ErrorMessage = FirstNameMaxLengthError)]
        [MinLength(FirstNameMinLength, ErrorMessage = FirstNameMinLengthError)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(LastNameMaxLength, ErrorMessage = LastNameMaxLengthError)]
        [MinLength(LastNameMinLength, ErrorMessage = LastNameMinLengthError)]
        [Display(Name = "Last Name")]

        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress(ErrorMessage = InvalidEmailError)]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;


        [Required]
        [MaxLength(PasswordmMaxLength, ErrorMessage = PasswordMaxLengthError)]
        [MinLength(PasswordMinLength, ErrorMessage = PasswordMinLengthError)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = PasswordsDoNotMatchError)]
        public string ConfirmPassword { get; set; } = null!;
    }
}
