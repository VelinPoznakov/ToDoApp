using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;

namespace TodoApp.Data.Configurations;

public class TodoEntityConfiguration: IEntityTypeConfiguration<TodoEntity>
{
    // Todos

    TodoEntity[] todos =
    {
        new TodoEntity
        {
            Id = Guid.Parse("5259ecb1-ea01-4b61-82ac-5a50d5c3d6b1"),
            Name = "Finish API",
            Description = "Complete the ASP.NET Core API",
            Priority = Priority.High,
            Status = Status.Pending,
            DueDate = new DateOnly(2026, 5, 30),
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            GroupId = Guid.Parse("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"),
            CreatedOn = new DateTime(2026, 5, 22, 10, 30, 0)
        },
        new TodoEntity
        {
            Id = Guid.Parse("9fc3138f-6c8d-47a8-b7b9-192e5df38c7e"),
            Name = "Study Signals",
            Description = "Prepare for Signals and Systems exam",
            Priority = Priority.Medium,
            Status = Status.Pending,
            DueDate = new DateOnly(2026, 6, 5),
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            GroupId = Guid.Parse("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"),
            CreatedOn = new DateTime(2026, 5, 21, 14, 15, 0)
        },
        new TodoEntity
        {
            Id = Guid.Parse("af8c31f0-a176-427d-916e-8eb9bc1cb1e4"),
            Name = "Play Battlefield 1",
            Description = "Play and stream Battlefield 1",
            Priority = Priority.Low,
            Status = Status.Completed,
            DueDate = new DateOnly(2026, 5, 25),
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            GroupId = Guid.Parse("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"),
            CreatedOn = new DateTime(2026, 5, 20, 18, 45, 0)
        },
        new TodoEntity
        {
            Id = Guid.Parse("d35d20fb-d640-4095-90a4-a11a3cd7d608"),
            Name = "Buy groceries",
            Description = "Buy food and drinks",
            Priority = Priority.Medium,
            Status = Status.Pending,
            DueDate = new DateOnly(2026, 5, 23),
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            GroupId = Guid.Parse("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"),
            CreatedOn = new DateTime(2026, 5, 19, 9, 0, 0)
        },
        new TodoEntity
        {
            Id = Guid.Parse("142279f9-cc2c-4766-8a0e-c801174b4fdf"),
            Name = "Morning workout",
            Description = "Complete cardio and strength training",
            Priority = Priority.High,
            Status = Status.Pending,
            DueDate = new DateOnly(2026, 5, 24),
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            GroupId = Guid.Parse("b4c7c90c-2094-4b50-b3a0-3b123a1583f6"),
            CreatedOn = new DateTime(2026, 5, 18, 7, 20, 0)
    }
};

    public void Configure(EntityTypeBuilder<TodoEntity> entity)
    {
        entity
            .HasQueryFilter(t => t.Status == Status.Pending)
            .HasData(todos);

        entity
            .HasOne(g => g.Group)
            .WithMany(t => t.Todos)
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}