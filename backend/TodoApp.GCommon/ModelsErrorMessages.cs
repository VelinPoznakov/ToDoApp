namespace TodoApp.GCommon
{
    public static class ModelsErrorMessages
    {
        public const string FirstNameMaxLengthError = "First name cannot exceed {1} characters.";
        public const string FirstNameMinLengthError = "First name must be at least {1} characters long.";

        public const string LastNameMaxLengthError = "Last name cannot exceed {1} characters.";
        public const string LastNameMinLengthError = "Last name must be at least {1} characters long.";

        public const string InvalidEmailError = "Please enter a valid email address.";

        public const string PasswordMaxLengthError = "Password cannot exceed {1} characters.";
        public const string PasswordMinLengthError = "Password must be at least {1} characters long.";

        public const string PasswordsDoNotMatchError = "The password and confirmation password do not match.";
    }
}
