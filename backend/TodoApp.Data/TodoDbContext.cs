using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data.Configurations;
using TodoApp.Models.Data;

namespace TodoApp.Data
{
    public class TodoDbContext: IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {

        }

        public virtual DbSet<TodoEntity> Todos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(TodoEntityConfigurations).Assembly);

            base.OnModelCreating(builder);
        }
    }
}
