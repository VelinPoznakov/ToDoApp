using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Data.Repositories;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Tests.Todos;

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

        TodoEntity[] result = (await repo.GetAllTodoNoTracking(page: 0)).ToArray();

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

        string[] names = (await repo.GetAllTodoNoTracking(page: 0))
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

        TodoEntity[] result = (await repo.GetAllTodoNoTracking(page: 0, ignoreQueryFilter: true)).ToArray();

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
            page: 0,
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
    public async Task CountTodosAsync_IgnoreQueryFilter_LiftsQueryFilterSoCallerCanCountCompleted()
    {
        // Contract: ignoreQueryFilter only lifts the global query filter
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
            ignoreQueryFilter: true));
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
            page: 0,
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

    // ===== Pagination =====

    private static async Task<DbContextOptions<TodoDbContext>> SeedPendingTodosAsync(
        Guid userId, int count)
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();

        await using var seed = new TodoDbContext(options);

        seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
        for (int i = 1; i <= count; i++)
        {
            seed.Todos.Add(TestDb.NewTodo(
                groupId, userId, $"Todo {i}", dueDate: new DateOnly(2030, 1, 1).AddDays(i)));
        }

        await seed.SaveChangesAsync();

        return options;
    }

    [Fact]
    public async Task GetAllTodoNoTracking_NoPage_ReturnsTheFirstPageSize()
    {
        var options = await SeedPendingTodosAsync(Guid.NewGuid(), TodosDefaultPageSize + 1);

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity[] result = (await repo.GetAllTodoNoTracking()).ToArray();

        Assert.Equal(TodosDefaultPageSize, result.Length);
        Assert.Equal("Todo 1", result[0].Name);
    }

    [Fact]
    public async Task GetAllTodoNoTracking_Page_IsZeroBased_AndSkipsThePreviousPages()
    {
        var options = await SeedPendingTodosAsync(Guid.NewGuid(), TodosDefaultPageSize + 1);

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity todo = Assert.Single(await repo.GetAllTodoNoTracking(page: 1));

        Assert.Equal($"Todo {TodosDefaultPageSize + 1}", todo.Name);
    }

    [Fact]
    public async Task GetAllTodoNoTracking_FilterOnCompleted_NeedsIgnoreQueryFilter()
    {
        // The global Status == Pending filter is added to every query, so asking
        // for completed todos without lifting it can never return anything.
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "G"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Done", Status.Completed));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        Assert.Empty(await repo.GetAllTodoNoTracking(
            filterQuery: t => t.Status == Status.Completed));
        Assert.Single(await repo.GetAllTodoNoTracking(
            filterQuery: t => t.Status == Status.Completed,
            ignoreQueryFilter: true));
    }

    [Fact]
    public async Task GetAllTodoNoTracking_ProjectionWithoutGroup_LeavesGroupEmpty_EvenWithInclude()
    {
        // A Select decides what comes back, so Include alone cannot fill Group.
        var options = await SeedPendingTodosAsync(Guid.NewGuid(), 1);

        await using var ctx = new TodoDbContext(options);
        var repo = new TodoRepository(ctx);

        TodoEntity todo = Assert.Single(await repo.GetAllTodoNoTracking(
            projectionQuery: t => new TodoEntity { Id = t.Id, Name = t.Name },
            includeGroup: true));

        Assert.Null(todo.Group);
    }
}
