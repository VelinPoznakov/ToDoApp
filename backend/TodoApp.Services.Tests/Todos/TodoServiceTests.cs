using System.Linq.Expressions;
using Moq;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;
using TodoApp.Services.Core;
using TodoApp.Services.Dtos;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Tests.Todos;

// Unit tests for TodoService. Both repositories are mocked so only the
// service's own mapping / orchestration / failure-translation is tested.
// What the queries actually return is covered by TodoServiceQueryTests.
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

    private void SetupGetTodo(TodoEntity? todo)
        => _todoRepo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);

    private static CreateEditTodoDto SampleDto() => new CreateEditTodoDto
    {
        Name = "X",
        Description = "Y",
        Priority = "Low",
        DueDate = "01-01-2027",
        GroupId = Guid.NewGuid()
    };

    // ===== GetTodoDetailsAsync =====

    [Fact]
    public async Task GetTodoDetails_WhenMissing_ReturnsNull()
    {
        SetupGetTodo(null);

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
        SetupGetTodo(todo);

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
        Assert.Equal("Nice", Assert.Single(result.Comments).Content);
    }

    [Fact]
    public async Task GetTodoDetails_Tracked_MapsOnlyEditableFields()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, groupId);
        SetupGetTodo(todo);

        var service = CreateService();
        TodoDetailsDto? result = await service.GetTodoDetailsAsync(userId, todo.Id, track: true);

        Assert.NotNull(result);
        Assert.Equal("Sample", result!.Name);
        Assert.Equal(groupId, result.GroupId);
        Assert.Empty(result.Comments);
    }

    // ===== AddTodoAsync =====

    [Fact]
    public async Task AddTodo_BuildsPendingEntityFromDto()
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

        await Assert.ThrowsAsync<DataPersistFail>(() => service.AddTodoAsync(SampleDto(), Guid.NewGuid()));
    }

    // ===== EditTodoAsync =====

    [Fact]
    public async Task EditTodo_WhenMissing_ThrowsEntityNotFound()
    {
        SetupGetTodo(null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.EditTodoAsync(Guid.NewGuid(), Guid.NewGuid(), SampleDto()));
    }

    [Fact]
    public async Task EditTodo_UpdatesFieldsAndStampsUpdatedOn()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        SetupGetTodo(todo);
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

    [Fact]
    public async Task EditTodo_OnlyLooksAtPendingTodos()
    {
        // The controller clears the Pending and All lists on that assumption.
        SetupGetTodo(null);

        var service = CreateService();
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.EditTodoAsync(Guid.NewGuid(), Guid.NewGuid(), SampleDto()));

        _todoRepo.Verify(r => r.GetTodoAsync(
            It.IsAny<Expression<Func<TodoEntity, bool>>>(), false, true), Times.Once);
    }

    [Fact]
    public async Task EditTodo_WhenRepoFails_ThrowsDataPersistFail()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        SetupGetTodo(todo);
        _todoRepo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(() => service.EditTodoAsync(userId, todo.Id, SampleDto()));
    }

    // ===== CompleteTodo =====

    [Fact]
    public async Task CompleteTodo_SetsStatusCompleted()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        SetupGetTodo(todo);
        _todoRepo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.CompleteTodo(todo.Id, userId);

        Assert.Equal(Status.Completed, todo.Status);
    }

    [Fact]
    public async Task CompleteTodo_WhenMissing_ThrowsEntityNotFound()
    {
        SetupGetTodo(null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.CompleteTodo(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CompleteTodo_WhenRepoFails_ThrowsDataPersistFail()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        SetupGetTodo(todo);
        _todoRepo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(() => service.CompleteTodo(todo.Id, userId));
    }

    // ===== ActivateTodoAsync =====

    [Fact]
    public async Task ActivateTodo_SetsStatusPending_AndLooksPastTheQueryFilter()
    {
        // A completed todo is hidden by the global filter, so without
        // ignoreQueryFilter it could never be found and activated.
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        todo.Status = Status.Completed;
        SetupGetTodo(todo);
        _todoRepo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.ActivateTodoAsync(userId, todo.Id);

        Assert.Equal(Status.Pending, todo.Status);
        _todoRepo.Verify(r => r.GetTodoAsync(
            It.IsAny<Expression<Func<TodoEntity, bool>>>(), true, true), Times.Once);
    }

    [Fact]
    public async Task ActivateTodo_WhenMissing_ThrowsEntityNotFound()
    {
        SetupGetTodo(null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.ActivateTodoAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ===== DeleteTodoAsync =====

    [Fact]
    public async Task DeleteTodo_WhenMissing_ThrowsEntityNotFound()
    {
        SetupGetTodo(null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.DeleteTodoAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteTodo_DeletesThroughRepository()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        SetupGetTodo(todo);
        _todoRepo.Setup(r => r.DeleteTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.DeleteTodoAsync(userId, todo.Id);

        _todoRepo.Verify(r => r.DeleteTodoAsync(todo), Times.Once);
    }

    [Fact]
    public async Task DeleteTodo_WhenRepoFails_ThrowsDataPersistFail()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());
        SetupGetTodo(todo);
        _todoRepo.Setup(r => r.DeleteTodoAsync(todo)).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(() => service.DeleteTodoAsync(userId, todo.Id));
    }

    // ===== Invalid filters =====

    [Fact]
    public async Task GetTodosOrderByPriorityDueDate_UndefinedFilter_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetTodosOrderByPriorityDueDateAsync(
                Guid.NewGuid(), Guid.NewGuid(), 0, (TodosSearchParameter)5));
    }

    [Fact]
    public async Task CountTodos_UndefinedFilter_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CountTodos(Guid.NewGuid(), (TodosSearchParameter)5));
    }

    [Fact]
    public async Task GetTodosFromAllGroups_AllFilter_Throws()
    {
        // TodosByStatus guards this with NotFound before it reaches the service.
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetTodosFromAllGroups(0, Guid.NewGuid(), TodosSearchParameter.All));
    }
}
