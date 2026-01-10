using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Models;

namespace TodoApp.Infrastructure.Data
{
    public class AplicationDbContext : IdentityDbContext<ApplicationUser>
  {
        public AplicationDbContext(DbContextOptions options)
                : base(options)
        {
        }

        public DbSet<ToDo> ToDos { get; set; }
  }
}