using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Models.Data;

namespace TodoApp.Data.Configurations
{
    public class TodoEntityConfigurations : IEntityTypeConfiguration<TodoEntity>
    {
        public void Configure(EntityTypeBuilder<TodoEntity> entity)
        {
            entity.HasQueryFilter(t => !t.IsCompleted);
        }
    }
}
