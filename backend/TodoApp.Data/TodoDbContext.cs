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
        public virtual DbSet<Group> Groups { get; set; } = null!;
        public virtual DbSet<Comment> Comments { get; set; } = null!;
        public virtual DbSet<SupportMessage> SupportMessages { get; set; } = null!;
        public virtual DbSet<RegularUser> RegularUsers { get; set; } = null!;
        public virtual DbSet<AdminUser> AdminUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new GroupConfiguration());
            builder.ApplyConfiguration(new TodoEntityConfiguration());
            builder.ApplyConfiguration(new SupportMessageConfiguration());

            builder.Entity<ApplicationUser>().UseTptMappingStrategy();
            builder.Entity<AdminUser>().ToTable("AdminUsers");
            builder.Entity<RegularUser>().ToTable("RegularUsers");
            
            base.OnModelCreating(builder);
            
        }
    }
}
