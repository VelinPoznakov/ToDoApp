using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;

namespace TodoApp.Data.Configurations;

public class TodoEntityConfiguration: IEntityTypeConfiguration<TodoEntity>
{
    public void Configure(EntityTypeBuilder<TodoEntity> entity)
    {
        entity.HasQueryFilter(t => t.Status == Status.Pending);

        entity
            .HasOne(g => g.Group)
            .WithMany(t => t.Todos)
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}