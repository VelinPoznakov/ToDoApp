using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TodoApp.GCommon.Exceptions;

using static TodoApp.GCommon.ErrorMessages;

namespace TodoApp.Web.Infrastructure
{
    public static class IdentityConfiguration
    {
        public static void PasswordConfiguration(PasswordOptions options, IConfiguration configuration)
        {
            string? sectionName = "Identity:Password"
                ?? throw new SectionNotFound(SectionNotFoundErrorMessage);

            options.RequireDigit = configuration.GetValue<bool>($"{sectionName}:RequireDigit");
            options.RequiredLength = configuration.GetValue<int>($"{sectionName}:RequiredLength");
            options.RequireNonAlphanumeric = configuration.GetValue<bool>($"{sectionName}:RequireNonAlphanumeric");
            options.RequireUppercase = configuration.GetValue<bool>($"{sectionName}:RequireUppercase");
            options.RequireLowercase = configuration.GetValue<bool>($"{sectionName}:RequireLowercase");
            options.RequiredUniqueChars = configuration.GetValue<int>($"{sectionName}:RequiredUniqueChars");
        }

        public static void SignInConfiguration(SignInOptions options, IConfiguration configuration)
        {
            string? sectionName = "Identity:SignIn"
                ?? throw new SectionNotFound(SectionNotFoundErrorMessage);

            options.RequireConfirmedEmail = configuration.GetValue<bool>($"{sectionName}:RequireConfirmedEmail");
            options.RequireConfirmedPhoneNumber = configuration.GetValue<bool>($"{sectionName}:RequireConfirmedPhoneNumber");
            options.RequireConfirmedAccount = configuration.GetValue<bool>($"{sectionName}:RequireConfirmedAccount");
        }
        public static void LockoutConfiguration(LockoutOptions options, IConfiguration configuration)
        {
            string? sectionName = "Identity:Lockout"
                ?? throw new SectionNotFound(SectionNotFoundErrorMessage);

            options.AllowedForNewUsers = configuration.GetValue<bool>($"{sectionName}:AllowedForNewUsers");
            options.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuration.GetValue<int>($"{sectionName}:DefaultLockoutTimeSpanInMinutes"));
            options.MaxFailedAccessAttempts = configuration.GetValue<int>($"{sectionName}:MaxFailedAccessAttempts");
        }

        public static void UserConfiguration(UserOptions options, IConfiguration configuration)
        {
            string? sectionName = "Identity:User"
                ?? throw new SectionNotFound(SectionNotFoundErrorMessage);

            options.RequireUniqueEmail = configuration.GetValue<bool>($"{sectionName}:RequireUniqueEmail");
        }

    }
}
