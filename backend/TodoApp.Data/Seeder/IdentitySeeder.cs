using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TodoApp.Data.Seeder.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;

namespace TodoApp.Data.Seeder
{
    public class IdentitySeeder : IIdentitySeeder
    {
        private readonly string[] ApplicationRoles =
        {
            "Admin",
            "User",
            "Manager"
        };

        private readonly RoleManager<IdentityRole<Guid>> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;

        public IdentitySeeder(RoleManager<IdentityRole<Guid>> roleManager, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.configuration = configuration;
        }

        public async Task RoleSeeder()
        {
            foreach(string role in ApplicationRoles)
            {
                bool roleExists = await roleManager.RoleExistsAsync(role);
                if(!roleExists)
                {
                    IdentityRole<Guid> identityRole = new IdentityRole<Guid>(role);

                    IdentityResult result = await roleManager.CreateAsync(identityRole);

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        public async Task AdminSeeder()
        {
            string? email = configuration.GetValue<string>("Admin:Email")
                ?? throw new SectionNotFound();

            string? password = configuration.GetValue<string>("Admin:Password")
                ?? throw new SectionNotFound();

            ApplicationUser? adminUser = await userManager.FindByEmailAsync(email);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = "Velin",
                    LastName = "Poznakov"
                };

                IdentityResult result = await userManager.CreateAsync(adminUser, password);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException();
                }
            }

            bool userInRole = await userManager.IsInRoleAsync(adminUser, ApplicationRoles[0]);

            if(!userInRole)
            {
                IdentityResult result = await userManager.AddToRoleAsync(adminUser, ApplicationRoles[0]);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
