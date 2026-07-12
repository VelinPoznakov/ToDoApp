using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Data.Repositories;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;

namespace TodoApp.Services.Tests;

public class TodoRepositoryTests
{
    // ===== GetAllTodoNoTracking =====

    [Fact]
    public async Task GetAllTodoNoTracking_Default_HidesCompletedTodos()
    {
        // The global query filter (Status == Pending) must apply by default.
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Visible", Status.Pending));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Hidden", Status.Completed));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity[] result = (await repo.GetAllTodoNoTracking()).ToArray();

        Assert.Single(result);
        Assert.Equal("Visible", result[0].Name);
    }

    [Fact]
    public async Task GetAllTodoNoTracking_Default_OrdersByPriorityThenDueDate()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "LowEarly", priority: Priority.Low, dueDate: new DateOnly(2026, 1, 1)));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "HighLate", priority: Priority.High, dueDate: new DateOnly(2026, 12, 1)));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "HighEarly", priority: Priority.High, dueDate: new DateOnly(2026, 2, 1)));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Medium", priority: Priority.Medium, dueDate: new DateOnly(2026, 1, 1)));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        string[] names = (await repo.GetAllTodoNoTracking())
            .Select(t => t.Name)
            .ToArray();

        Assert.Equal(new[] { "HighEarly", "HighLate", "Medium", "LowEarly" }, names);
    }

    [Fact]
    public async Task GetAllTodoNoTracking_IgnoreQueryFilter_ReturnsCompletedToo_PendingFirst()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Done", Status.Completed, Priority.High));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Open", Status.Pending, Priority.Low));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity[] result = (await repo.GetAllTodoNoTracking(ignoreQueryFilter: true)).ToArray();

        Assert.Equal(2, result.Length);
        // Ordered by Status first: Pending (0) before Completed (1).
        Assert.Equal("Open", result[0].Name);
        Assert.Equal("Done", result[1].Name);
    }

    [Fact]
    public async Task GetAllTodoNoTracking_FilterAndProjection_ReturnProjectedMatch()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid otherGroupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "Target"));
            seed.Groups.Add(TestDb.NewGroup(otherGroupId, userId, "Other"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "InTarget"));
            seed.Todos.Add(TestDb.NewTodo(otherGroupId, userId, "InOther"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity[] result = (await repo.GetAllTodoNoTracking(
            filterQuery: t => t.GroupId == groupId,
            projectionQuery: t => new TodoEntity { Id = t.Id, Name = t.Name })).ToArray();

        Assert.Single(result);
        Assert.Equal("InTarget", result[0].Name);
    }

    // ===== GetTodoAsync =====

    [Fact]
    public async Task GetTodoAsync_NotTracking_LoadsGroupAndComments()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid todoId;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "WithNav"));
            TodoEntity todo = TestDb.NewTodo(groupId, userId, "HasComment");
            todoId = todo.Id;
            seed.Todos.Add(todo);
            seed.Comments.Add(new Comment { Content = "First!", TodoId = todo.Id, CreatedOn = DateTime.Now });
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity? result = await repo.GetTodoAsync(t => t.Id == todoId);

        Assert.NotNull(result);
        Assert.Equal("WithNav", result!.Group.Name);
        Assert.Single(result.Comments);
    }

    [Fact]
    public async Task GetTodoAsync_Default_CompletedTodoIsHiddenByQueryFilter()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid todoId;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
            TodoEntity todo = TestDb.NewTodo(groupId, userId, "Done", Status.Completed);
            todoId = todo.Id;
            seed.Todos.Add(todo);
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        Assert.Null(await repo.GetTodoAsync(t => t.Id == todoId));
        Assert.NotNull(await repo.GetTodoAsync(t => t.Id == todoId, ignoreQueryFilter: true));
    }

    // ===== Add / Edit / Delete =====

    [Fact]
    public async Task AddTodoAsync_PersistsTheTodo()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        TodoEntity todo = TestDb.NewTodo(groupId, userId, "Fresh");

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new TodoRepository(ctx);
            Assert.True(await repo.AddTodoAsync(todo));
        }

        await using var verify = new TodoDbContext(options);
        Assert.NotNull(await verify.Todos.FirstOrDefaultAsync(t => t.Id == todo.Id));
    }

    [Fact]
    public async Task EditTodoAsync_PersistsChanges()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        TodoEntity todo = TestDb.NewTodo(groupId, userId, "Before");

        await using (var seed = new TodoDbContext(options))
        {
            seed.Todos.Add(todo);
            await seed.SaveChangesAsync();
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new TodoRepository(ctx);
            TodoEntity tracked = (await repo.GetTodoAsync(t => t.Id == todo.Id, tracking: true))!;
            tracked.Name = "After";

            Assert.True(await repo.EditTodoAsync(tracked));
        }

        await using var verify = new TodoDbContext(options);
        Assert.Equal("After", (await verify.Todos.FirstAsync(t => t.Id == todo.Id)).Name);
    }

    [Fact]
    public async Task DeleteTodoAsync_RemovesTheTodo()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        TodoEntity todo = TestDb.NewTodo(groupId, userId, "Doomed");

        await using (var seed = new TodoDbContext(options))
        {
            seed.Todos.Add(todo);
            await seed.SaveChangesAsync();
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new TodoRepository(ctx);
            TodoEntity tracked = (await repo.GetTodoAsync(t => t.Id == todo.Id, tracking: true))!;

            Assert.True(await repo.DeleteTodoAsync(tracked));
        }

        await using var verify = new TodoDbContext(options);
        Assert.Null(await verify.Todos.FirstOrDefaultAsync(t => t.Id == todo.Id));
    }

    // ===== CountTodosAsync =====

    [Fact]
    public async Task CountTodosAsync_Default_CountsOnlyPending()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "P1", Status.Pending));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "P2", Status.Pending));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "C1", Status.Completed));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        Assert.Equal(2, await repo.CountTodosAsync(t => t.UserId == userId));
    }

    [Fact]
    public async Task CountTodosAsync_CountCompleted_LiftsQueryFilterSoCallerCanCountCompleted()
    {
        // Contract: countCompleted only lifts the global query filter
        // (Status == Pending); the STATUS condition itself is the caller's
        // job (TodoService passes t.Status == Status.Completed).
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "P1", Status.Pending));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "P2", Status.Pending));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "C1", Status.Completed));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        Assert.Equal(1, await repo.CountTodosAsync(
            t => t.UserId == userId && t.Status == Status.Completed,
            countCompleted: true));
    }

    // ===== GetAllTodoNoTracking with includeGroup =====

    [Fact]
    public async Task GetAllTodoNoTracking_IncludeGroup_LoadsTheGroupNavigation()
    {
        // GetTodosAfterDueDate relies on this path to show the group name
        // on the dashboard.
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "Dashboard Group"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Overdue"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity[] result = (await repo.GetAllTodoNoTracking(
            filterQuery: t => t.UserId == userId,
            projectionQuery: t => new TodoEntity
            {
                Id = t.Id,
                Name = t.Name,
                Group = t.Group
            },
            includeGroup: true)).ToArray();

        TodoEntity todo = Assert.Single(result);
        Assert.NotNull(todo.Group);
        Assert.Equal("Dashboard Group", todo.Group.Name);
    }
}
