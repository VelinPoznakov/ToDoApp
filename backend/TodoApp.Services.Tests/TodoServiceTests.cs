using System.Linq.Expressions;
using Moq;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;
using TodoApp.Services.Core;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Tests;

// Unit tests for TodoService. Both repositories are mocked so only the
// service's own mapping / orchestration / failure-translation is tested.
public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _todoRepo = new();
    private readonly Mock<IGroupRepository> _groupRepo = new();

    private TodoService CreateService() => new TodoService(_todoRepo.Object, _groupRepo.Object);

    private static TodoEntity SampleTodo(Guid userId, Guid groupId, Guid? id = null)
        => new TodoEntity
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Sample",
            Description = "Sample description",
            Priority = Priority.High,
            Status = Status.Pending,
            DueDate = new DateOnly(2026, 7, 20),
            CreatedOn = new DateTime(2026, 7, 1, 10, 0, 0),
            UserId = userId,
            GroupId = groupId,
            Group = new Group { Id = groupId, Name = "The Group", UserId = userId }
        };

    // Setup helper: the three list methods all resolve the group name via GetGroupById.
    private void SetupGroupName(Guid groupId, string name)
        => _groupRepo.Setup(r => r.GetGroupById(
                groupId,
                It.IsAny<Expression<Func<Group, Group>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new Group { Id = groupId, Name = name });

    private void SetupTodos(params TodoEntity[] todos)
        => _todoRepo.Setup(r => r.GetAllTodoNoTracking(
                It.IsAny<int>(),
                It.IsAny<Expression<Func<TodoEntity, bool>>?>(),
                It.IsAny<Expression<Func<TodoEntity, TodoEntity>>?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(todos);

    // ===== GetAllTodosOrderByPriorityDueDateAsync (All tab) =====

    [Fact]
    public async Task GetAllTodos_MapsEnumsAndDate_AndReturnsGroupName()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        SetupTodos(SampleTodo(userId, groupId));
        SetupGroupName(groupId, "Work");

        var service = CreateService();
        PagedTodosDto page = await service
            .GetAllTodosOrderByPriorityDueDateAsync(userId, groupId, 0);

        AllTodoDto dto = Assert.Single(page.Todos);
        Assert.Equal("Sample", dto.Name);
        Assert.Equal("High", dto.Priority);
        Assert.Equal("Pending", dto.Status);
        Assert.Equal("20-07-2026", dto.DueDate);
        Assert.Equal("Work", page.GroupName);
        Assert.Equal(0, page.PageNumber);
    }

    [Fact]
    public async Task GetAllTodos_WhenGroupMissing_ThrowsEntityNotFound()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        SetupTodos();
        _groupRepo.Setup(r => r.GetGroupById(
                groupId,
                It.IsAny<Expression<Func<Group, Group>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Group?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.GetAllTodosOrderByPriorityDueDateAsync(userId, groupId, 0));
    }

    // ===== Pending tab =====

    [Fact]
    public async Task GetAllPending_ReturnsMappedTodosAndGroupName()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        SetupTodos(SampleTodo(userId, groupId));
        SetupGroupName(groupId, "Pending Group");

        var service = CreateService();
        PagedTodosDto page = await service
            .GetAllPendingTodosOrderByPriorityDueDateAsync(userId, groupId, 0);

        Assert.Single(page.Todos);
        Assert.Equal("Pending Group", page.GroupName);
    }

    [Fact]
    public async Task GetAllPending_WhenGroupMissing_ThrowsEntityNotFound()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        SetupTodos();
        _groupRepo.Setup(r => r.GetGroupById(
                groupId,
                It.IsAny<Expression<Func<Group, Group>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Group?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.GetAllPendingTodosOrderByPriorityDueDateAsync(userId, groupId, 0));
    }

    // ===== Completed tab =====

    [Fact]
    public async Task GetAllCompleted_ReturnsMappedTodosAndGroupName()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        TodoEntity done = SampleTodo(userId, groupId);
        done.Status = Status.Completed;
        SetupTodos(done);
        SetupGroupName(groupId, "Completed Group");

        var service = CreateService();
        PagedTodosDto page = await service
            .GetAllCompletedOrderByPriorityDueDateAsync(userId, groupId, 0);

        AllTodoDto dto = Assert.Single(page.Todos);
        Assert.Equal("Completed", dto.Status);
        Assert.Equal("Completed Group", page.GroupName);
    }

    [Fact]
    public async Task GetAllCompleted_WhenGroupMissing_ThrowsEntityNotFound()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        SetupTodos();
        _groupRepo.Setup(r => r.GetGroupById(
                groupId,
                It.IsAny<Expression<Func<Group, Group>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Group?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.GetAllCompletedOrderByPriorityDueDateAsync(userId, groupId, 0));
    }

    // ===== GetTodoDetailsAsync =====

    [Fact]
    public async Task GetTodoDetails_WhenMissing_ReturnsNull()
    {
        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync((TodoEntity?)null);

        var service = CreateService();

        Assert.Null(await service.GetTodoDetailsAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetTodoDetails_NotTracked_MapsFullDetails()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, groupId);
        todo.Comments.Add(new Comment { Id = 1, Content = "Nice", TodoId = todo.Id });

        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);

        var service = CreateService();
        TodoDetailsDto? result = await service.GetTodoDetailsAsync(userId, todo.Id);

        Assert.NotNull(result);
        Assert.Equal("Sample", result!.Name);
        Assert.Equal("Sample description", result.Description);
        Assert.Equal("High", result.Priority);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("20-07-2026", result.DueDate);
        Assert.Equal("01-07-2026", result.CreatedOn);
        Assert.Equal("The Group", result.GroupName);
        Assert.Equal(groupId, result.GroupId);
        Assert.Single(result.Comments);
        Assert.Equal("Nice", result.Comments[0].Content);
    }

    [Fact]
    public async Task GetTodoDetails_Tracked_MapsOnlyEditableFields()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, groupId);

        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);

        var service = CreateService();
        TodoDetailsDto? result = await service.GetTodoDetailsAsync(userId, todo.Id, track: true);

        Assert.NotNull(result);
        Assert.Equal("Sample", result!.Name);
        Assert.Equal(groupId, result.GroupId);
        Assert.Empty(result.Comments);
    }

    // ===== AddTodoAsync =====

    [Fact]
    public async Task AddTodo_BuildsEntityFromDto()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        TodoEntity? captured = null;

        _todoRepo.Setup(r => r.AddTodoAsync(It.IsAny<TodoEntity>()))
             .Callback<TodoEntity>(t => captured = t)
             .ReturnsAsync(true);

        var service = CreateService();
        await service.AddTodoAsync(new CreateEditTodoDto
        {
            Name = "New todo",
            Description = "Do it",
            Priority = "Medium",
            DueDate = "25-12-2026",
            GroupId = groupId
        }, userId);

        Assert.NotNull(captured);
        Assert.Equal("New todo", captured!.Name);
        Assert.Equal(Priority.Medium, captured.Priority);
        Assert.Equal(Status.Pending, captured.Status);
        Assert.Equal(new DateOnly(2026, 12, 25), captured.DueDate);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal(groupId, captured.GroupId);
    }

    [Fact]
    public async Task AddTodo_WhenRepoFails_ThrowsDataPersistFail()
    {
        _todoRepo.Setup(r => r.AddTodoAsync(It.IsAny<TodoEntity>())).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(() => service.AddTodoAsync(new CreateEditTodoDto
        {
            Name = "X",
            Description = "Y",
            Priority = "Low",
            DueDate = "01-01-2027",
            GroupId = Guid.NewGuid()
        }, Guid.NewGuid()));
    }

    // ===== EditTodoAsync =====

    [Fact]
    public async Task EditTodo_WhenMissing_ThrowsEntityNotFound()
    {
        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync((TodoEntity?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.EditTodoAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateEditTodoDto
            {
                Name = "X",
                Description = "Y",
                Priority = "Low",
                DueDate = "01-01-2027"
            }));
    }

    [Fact]
    public async Task EditTodo_UpdatesFieldsAndStampsUpdatedOn()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());

        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _todoRepo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.EditTodoAsync(userId, todo.Id, new CreateEditTodoDto
        {
            Name = "Renamed",
            Description = "New text",
            Priority = "Low",
            DueDate = "31-12-2026"
        });

        Assert.Equal("Renamed", todo.Name);
        Assert.Equal("New text", todo.Description);
        Assert.Equal(Priority.Low, todo.Priority);
        Assert.Equal(new DateOnly(2026, 12, 31), todo.DueDate);
        Assert.NotNull(todo.UpdatedOn);
        _todoRepo.Verify(r => r.EditTodoAsync(todo), Times.Once);
    }

    // ===== Complete / Activate =====

    [Fact]
    public async Task CompleteTodo_SetsStatusCompleted()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());

        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _todoRepo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.CompleteTodo(todo.Id, userId);

        Assert.Equal(Status.Completed, todo.Status);
    }

    [Fact]
    public async Task ActivateTodo_SetsStatusPending()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        todo.Status = Status.Completed;

        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _todoRepo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.ActivateTodoAsync(userId, todo.Id);

        Assert.Equal(Status.Pending, todo.Status);
    }

    [Fact]
    public async Task CompleteTodo_WhenMissing_ThrowsEntityNotFound()
    {
        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync((TodoEntity?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.CompleteTodo(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ===== DeleteTodoAsync =====

    [Fact]
    public async Task DeleteTodo_WhenMissing_ThrowsEntityNotFound()
    {
        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync((TodoEntity?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.DeleteTodoAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteTodo_DeletesThroughRepository()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());

        _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _todoRepo.Setup(r => r.DeleteTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.DeleteTodoAsync(userId, todo.Id);

        _todoRepo.Verify(r => r.DeleteTodoAsync(todo), Times.Once);
    }

    // ===== CountTodos =====

    [Fact]
    public async Task CountTodos_OnlyCompleted_PassesCountCompletedTrue()
    {
        Guid userId = Guid.NewGuid();

        _todoRepo.Setup(r => r.CountTodosAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    true))
             .ReturnsAsync(3);

        var service = CreateService();

        Assert.Equal(3, await service.CountTodos(userId, true));
        _todoRepo.Verify(r => r.CountTodosAsync(
            It.IsAny<Expression<Func<TodoEntity, bool>>>(), true), Times.Once);
    }

    [Fact]
    public async Task CountTodos_Default_PassesCountCompletedFalse()
    {
        Guid userId = Guid.NewGuid();

        _todoRepo.Setup(r => r.CountTodosAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    false))
             .ReturnsAsync(5);

        var service = CreateService();

        Assert.Equal(5, await service.CountTodos(userId));
        _todoRepo.Verify(r => r.CountTodosAsync(
            It.IsAny<Expression<Func<TodoEntity, bool>>>(), false), Times.Once);
    }

    // ===== GetTodosAfterDueDate =====

    [Fact]
    public async Task GetTodosAfterDueDate_MapsGroupName()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());

        _todoRepo.Setup(r => r.GetAllTodoNoTracking(
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<TodoEntity, bool>>?>(),
                    It.IsAny<Expression<Func<TodoEntity, TodoEntity>>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(new[] { todo });

        var service = CreateService();
        TodosAfterDuelDate[] result = (await service.GetTodosAfterDueDate(userId)).ToArray();

        Assert.Single(result);
        Assert.Equal("The Group", result[0].GroupName);
        Assert.Equal("20-07-2026", result[0].DueDate);
    }
}
