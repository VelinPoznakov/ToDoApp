using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;

namespace TodoApp.Services.Tests;

// Shared helpers for repository tests running on EF Core InMemory.
public static class TestDb
{
    public static DbContextOptions<TodoDbContext> Options()
        => new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    public static Group NewGroup(Guid id, Guid userId, string name)
        => new Group
        {
            Id = id,
            Name = name,
            UserId = userId,
            CreatedOn = DateTime.Now
        };

    public static TodoEntity NewTodo(
        Guid groupId,
        Guid userId,
        string name,
        Status status = Status.Pending,
        Priority priority = Priority.Medium,
        DateOnly? dueDate = null)
        => new TodoEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "description",
            Priority = priority,
            Status = status,
            DueDate = dueDate ?? new DateOnly(2026, 6, 15),
            UserId = userId,
            GroupId = groupId,
            CreatedOn = DateTime.Now
        };
}
