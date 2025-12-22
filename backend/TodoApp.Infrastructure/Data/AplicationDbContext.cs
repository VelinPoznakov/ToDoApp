using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Models;

namespace TodoApp.Infrastructure.Data
{
    public class AplicationDbContext : DbContext
  {
        public AplicationDbContext(DbContextOptions options)
                : base(options)
        {
        }

        public DbSet<ToDo> ToDos { get; set; }
  }
}