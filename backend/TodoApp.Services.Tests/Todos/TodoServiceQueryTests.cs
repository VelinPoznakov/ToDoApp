using TodoApp.Data;
using TodoApp.Data.Repositories;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;
using TodoApp.Services.Core;
using TodoApp.Services.Dtos;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Tests.Todos;

// TodoService on top of the real repositories and EF Core InMemory. The filter,
// the global query filter and the projection only work (or break) together, so
// mocked repositories cannot catch bugs like a Completed tab that is always
// empty or a group name that is always blank.
public class TodoServiceQueryTests : IDisposable
{
    private readonly TodoDbContext _context = new TodoDbContext(TestDb.Options());
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _workGroupId = Guid.NewGuid();
    private readonly Guid _homeGroupId = Guid.NewGuid();
    private readonly Guid _emptyGroupId = Guid.NewGuid();
    private readonly Guid _otherUsersGroupId = Guid.NewGuid();

    public TodoServiceQueryTests()
    {
        _context.Groups.Add(TestDb.NewGroup(_workGroupId, _userId, "Work"));
        _context.Groups.Add(TestDb.NewGroup(_homeGroupId, _userId, "Home"));
        _context.Groups.Add(TestDb.NewGroup(_emptyGroupId, _userId, "Empty"));
        _context.Groups.Add(TestDb.NewGroup(_otherUsersGroupId, _otherUserId, "Theirs"));
    }

    public void Dispose() => _context.Dispose();

    private TodoService CreateService() => new TodoService(
        new TodoRepository(_context),
        new GroupRepository(_context));

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task SeedTodosAsync(params TodoEntity[] todos)
    {
        _context.Todos.AddRange(todos);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private Task SeedMixedTodosAsync() => SeedTodosAsync(
        TestDb.NewTodo(_workGroupId, _userId, "Work pending", Status.Pending, dueDate: Today.AddDays(5)),
        TestDb.NewTodo(_workGroupId, _userId, "Work completed", Status.Completed, dueDate: Today.AddDays(5)),
        TestDb.NewTodo(_homeGroupId, _userId, "Home pending", Status.Pending, dueDate: Today.AddDays(5)),
        TestDb.NewTodo(_homeGroupId, _userId, "Home completed", Status.Completed, dueDate: Today.AddDays(5)),
        TestDb.NewTodo(_otherUsersGroupId, _otherUserId, "Their pending", Status.Pending, dueDate: Today.AddDays(5)),
        TestDb.NewTodo(_otherUsersGroupId, _otherUserId, "Their completed", Status.Completed, dueDate: Today.AddDays(5)));

    private async Task SeedManyPendingTodosAsync(int count)
    {
        TodoEntity[] todos = Enumerable.Range(1, count)
            .Select(i => TestDb.NewTodo(_workGroupId, _userId, $"Todo {i}", dueDate: Today.AddDays(i)))
            .ToArray();

        await SeedTodosAsync(todos);
    }

    // ===== GetTodosOrderByPriorityDueDateAsync (the tabs of one group) =====

    [Theory]
    [InlineData(TodosSearchParameter.All, new[] { "Work completed", "Work pending" })]
    [InlineData(TodosSearchParameter.Pending, new[] { "Work pending" })]
    [InlineData(TodosSearchParameter.Completed, new[] { "Work completed" })]
    public async Task GroupTodos_EachTab_ReturnsOnlyItsTodos_FromThatGroup(
        TodosSearchParameter filter, string[] expected)
    {
        // Regression: the global Status == Pending filter stayed on for the
        // Completed tab, so that tab was always empty.
        await SeedMixedTodosAsync();

        var service = CreateService();
        PagedTodosDto<AllTodoDto> page = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _workGroupId, 0, filter);

        Assert.Equal(expected, page.Todos.Select(t => t.Name).OrderBy(n => n));
    }

    [Theory]
    [InlineData(TodosSearchParameter.All)]
    [InlineData(TodosSearchParameter.Pending)]
    [InlineData(TodosSearchParameter.Completed)]
    public async Task GroupTodos_PageWithTodos_ReturnsTheGroupName(TodosSearchParameter filter)
    {
        // Regression: the projection did not select Group, so the name was blank.
        await SeedMixedTodosAsync();

        var service = CreateService();
        PagedTodosDto<AllTodoDto> page = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _workGroupId, 0, filter);

        Assert.Equal("Work", page.GroupName);
    }

    [Fact]
    public async Task GroupTodos_EmptyGroup_HasABlankGroupName()
    {
        // Known limitation: the name is read from the first todo of the page,
        // so a page without todos has nothing to read it from.
        await _context.SaveChangesAsync();

        var service = CreateService();
        PagedTodosDto<AllTodoDto> page = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _emptyGroupId, 0, TodosSearchParameter.All);

        Assert.Empty(page.Todos);
        Assert.Equal(string.Empty, page.GroupName);
    }

    [Fact]
    public async Task GroupTodos_AnotherUsersGroup_ReturnsNothing()
    {
        await SeedMixedTodosAsync();

        var service = CreateService();
        PagedTodosDto<AllTodoDto> page = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _otherUsersGroupId, 0, TodosSearchParameter.All);

        Assert.Empty(page.Todos);
    }

    [Fact]
    public async Task GroupTodos_MapsEnumsAndDate()
    {
        await SeedTodosAsync(TestDb.NewTodo(
            _workGroupId, _userId, "Mapped", Status.Pending, Priority.High, new DateOnly(2030, 7, 20)));

        var service = CreateService();
        PagedTodosDto<AllTodoDto> page = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _workGroupId, 0, TodosSearchParameter.All);

        AllTodoDto dto = Assert.Single(page.Todos);
        Assert.Equal("High", dto.Priority);
        Assert.Equal("Pending", dto.Status);
        Assert.Equal("20-07-2030", dto.DueDate);
    }

    [Fact]
    public async Task GroupTodos_Paging_SplitsTodosIntoPagesWithoutOverlap()
    {
        await SeedManyPendingTodosAsync(TodosDefaultPageSize + 2);

        var service = CreateService();
        PagedTodosDto<AllTodoDto> first = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _workGroupId, 0, TodosSearchParameter.All);
        PagedTodosDto<AllTodoDto> second = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _workGroupId, 1, TodosSearchParameter.All);

        Assert.Equal(TodosDefaultPageSize, first.Todos.Count());
        Assert.Equal(2, second.Todos.Count());
        Assert.Empty(first.Todos.Select(t => t.Id).Intersect(second.Todos.Select(t => t.Id)));
        Assert.Equal(1, second.PageNumber);
    }

    // ===== GetTodosFromAllGroups (dashboard cards) =====

    [Theory]
    [InlineData(TodosSearchParameter.Pending, new[] { "Home pending", "Work pending" })]
    [InlineData(TodosSearchParameter.Completed, new[] { "Home completed", "Work completed" })]
    public async Task AllGroups_ReturnsTheStatusAcrossGroups_ForThisUserOnly(
        TodosSearchParameter filter, string[] expected)
    {
        await SeedMixedTodosAsync();

        var service = CreateService();
        PagedTodosDto<TodosAfterDuelDate> page = await service.GetTodosFromAllGroups(0, _userId, filter);

        Assert.Equal(expected, page.Todos.Select(t => t.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task AllGroups_EveryRowCarriesItsOwnGroup()
    {
        await SeedMixedTodosAsync();

        var service = CreateService();
        PagedTodosDto<TodosAfterDuelDate> page = await service
            .GetTodosFromAllGroups(0, _userId, TodosSearchParameter.Pending);

        TodosAfterDuelDate work = page.Todos.Single(t => t.Name == "Work pending");
        TodosAfterDuelDate home = page.Todos.Single(t => t.Name == "Home pending");

        Assert.Equal("Work", work.GroupName);
        Assert.Equal(_workGroupId, work.GroupId);
        Assert.Equal("Home", home.GroupName);
        Assert.Equal(_homeGroupId, home.GroupId);
    }

    [Fact]
    public async Task AllGroups_Paging_ReturnsTheRequestedPage()
    {
        await SeedManyPendingTodosAsync(TodosDefaultPageSize + 3);

        var service = CreateService();
        PagedTodosDto<TodosAfterDuelDate> second = await service
            .GetTodosFromAllGroups(1, _userId, TodosSearchParameter.Pending);

        Assert.Equal(3, second.Todos.Count());
    }

    // ===== CountTodos =====

    [Theory]
    [InlineData(TodosSearchParameter.All, 4)]
    [InlineData(TodosSearchParameter.Pending, 2)]
    [InlineData(TodosSearchParameter.Completed, 2)]
    public async Task CountTodos_NoGroup_CountsAllOfTheUsersGroups(TodosSearchParameter filter, int expected)
    {
        await SeedMixedTodosAsync();

        var service = CreateService();

        Assert.Equal(expected, await service.CountTodos(_userId, filter));
    }

    [Theory]
    [InlineData(TodosSearchParameter.All, 2)]
    [InlineData(TodosSearchParameter.Pending, 1)]
    [InlineData(TodosSearchParameter.Completed, 1)]
    public async Task CountTodos_WithGroup_CountsOnlyThatGroup(TodosSearchParameter filter, int expected)
    {
        await SeedMixedTodosAsync();

        var service = CreateService();

        Assert.Equal(expected, await service.CountTodos(_userId, filter, _workGroupId));
    }

    [Theory]
    [InlineData(TodosSearchParameter.All)]
    [InlineData(TodosSearchParameter.Pending)]
    [InlineData(TodosSearchParameter.Completed)]
    public async Task CountTodos_MatchesTheNumberOfTodosTheListReturns(TodosSearchParameter filter)
    {
        // Regression: the page buttons come from the count and the rows from the
        // list. When they disagreed, the tabs showed extra empty pages.
        await SeedMixedTodosAsync();

        var service = CreateService();
        int count = await service.CountTodos(_userId, filter, _workGroupId);
        PagedTodosDto<AllTodoDto> page = await service
            .GetTodosOrderByPriorityDueDateAsync(_userId, _workGroupId, 0, filter);

        Assert.Equal(count, page.Todos.Count());
    }

    // ===== GetTodosAfterDueDate (dashboard list) =====

    [Fact]
    public async Task AfterDueDate_ReturnsOnlyOverduePendingTodosOfThisUser()
    {
        await SeedTodosAsync(
            TestDb.NewTodo(_workGroupId, _userId, "Overdue", Status.Pending, dueDate: Today.AddDays(-3)),
            TestDb.NewTodo(_workGroupId, _userId, "Due today", Status.Pending, dueDate: Today),
            TestDb.NewTodo(_workGroupId, _userId, "Due later", Status.Pending, dueDate: Today.AddDays(3)),
            TestDb.NewTodo(_workGroupId, _userId, "Overdue but done", Status.Completed, dueDate: Today.AddDays(-3)),
            TestDb.NewTodo(_otherUsersGroupId, _otherUserId, "Their overdue", Status.Pending, dueDate: Today.AddDays(-3)));

        var service = CreateService();
        TodosAfterDuelDate todo = Assert.Single(await service.GetTodosAfterDueDate(_userId));

        Assert.Equal("Overdue", todo.Name);
        Assert.Equal("Work", todo.GroupName);
        Assert.Equal(_workGroupId, todo.GroupId);
    }

    [Fact]
    public async Task AfterDueDate_ReturnsTheFirstPage_NotTheSecond()
    {
        // Regression: page: 1 skipped the first 10 todos after pagination became
        // 0-based, so with 10 or fewer overdue todos the dashboard stayed empty.
        await SeedTodosAsync(
            TestDb.NewTodo(_workGroupId, _userId, "Overdue", Status.Pending, dueDate: Today.AddDays(-1)));

        var service = CreateService();

        Assert.Single(await service.GetTodosAfterDueDate(_userId));
    }
}
