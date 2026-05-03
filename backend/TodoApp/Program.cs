using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Data.Seeder;
using TodoApp.Data.Seeder.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Web.Infrastructure;
using static TodoApp.Web.Infrastructure.IdentityConfiguration;

namespace TodoApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            string? connectionString = builder
                .Configuration
                .GetConnectionString("DevSqlServer")
                ?? throw new ConnectionStringNotFound("Connection string is not found");

            builder.Services.AddDbContext<TodoDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddTransient<IIdentitySeeder, IdentitySeeder>();

            builder.Services
                .AddDefaultIdentity<ApplicationUser>(options =>
                {
                    PasswordConfiguration(options.Password, builder.Configuration);
                    SignInConfiguration(options.SignIn, builder.Configuration);
                    LockoutConfiguration(options.Lockout, builder.Configuration);
                    UserConfiguration(options.User, builder.Configuration);
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<TodoDbContext>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                options.SlidingExpiration = true;
                options.LoginPath = "/User/Login";
                options.LogoutPath = "/User/Logout";
                options.AccessDeniedPath = "/Home/Index";
            });

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            builder.Services.AddAuthorization();

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.SeedRoles();
            app.SeedAdmin();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
