using System.Linq.Expressions;
using Moq;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;
using TodoApp.Services.Core;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Tests;

public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _repo = new();

    private TodoService CreateService() => new TodoService(_repo.Object);

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

    // ===== GetAllTodosOrderByPriorityDueDateAsync =====

    [Fact]
    public async Task GetAllTodos_MapsEnumsAndDateToStrings()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        _repo.Setup(r => r.GetAllTodoNoTracking(
                    It.IsAny<Expression<Func<TodoEntity, bool>>?>(),
                    It.IsAny<Expression<Func<TodoEntity, TodoEntity>>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(new[] { SampleTodo(userId, groupId) });

        var service = CreateService();
        AllTodoDto[] result =
            (await service.GetAllTodosOrderByPriorityDueDateAsync(userId, groupId)).ToArray();

        Assert.Single(result);
        Assert.Equal("Sample", result[0].Name);
        Assert.Equal("High", result[0].Priority);
        Assert.Equal("Pending", result[0].Status);
        Assert.Equal("20-07-2026", result[0].DueDate);
    }

    // ===== GetTodoDetailsAsync =====

    [Fact]
    public async Task GetTodoDetails_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetTodoAsync(
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

        _repo.Setup(r => r.GetTodoAsync(
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

        _repo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);

        var service = CreateService();
        TodoDetailsDto? result = await service.GetTodoDetailsAsync(userId, todo.Id, track: true);

        Assert.NotNull(result);
        Assert.Equal("Sample", result!.Name);
        Assert.Equal(groupId, result.GroupId);
        // Details-only fields are not populated in tracked (edit) mode.
        Assert.Empty(result.Comments);
    }

    // ===== AddTodoAsync =====

    [Fact]
    public async Task AddTodo_BuildsEntityFromDto()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        TodoEntity? captured = null;

        _repo.Setup(r => r.AddTodoAsync(It.IsAny<TodoEntity>()))
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
        _repo.Setup(r => r.AddTodoAsync(It.IsAny<TodoEntity>())).ReturnsAsync(false);

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
        _repo.Setup(r => r.GetTodoAsync(
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

        _repo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _repo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

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
        _repo.Verify(r => r.EditTodoAsync(todo), Times.Once);
    }

    // ===== Complete / Activate =====

    [Fact]
    public async Task CompleteTodo_SetsStatusCompleted()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());

        _repo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _repo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

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

        _repo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _repo.Setup(r => r.EditTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.ActivateTodoAsync(userId, todo.Id);

        Assert.Equal(Status.Pending, todo.Status);
    }

    [Fact]
    public async Task CompleteTodo_WhenMissing_ThrowsEntityNotFound()
    {
        _repo.Setup(r => r.GetTodoAsync(
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
        _repo.Setup(r => r.GetTodoAsync(
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

        _repo.Setup(r => r.GetTodoAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(todo);
        _repo.Setup(r => r.DeleteTodoAsync(todo)).ReturnsAsync(true);

        var service = CreateService();
        await service.DeleteTodoAsync(userId, todo.Id);

        _repo.Verify(r => r.DeleteTodoAsync(todo), Times.Once);
    }

    // ===== CountTodos / GetTodosAfterDueDate =====

    [Fact]
    public async Task CountTodos_PassesOnlyCompletedFlag()
    {
        Guid userId = Guid.NewGuid();

        _repo.Setup(r => r.CountTodosAsync(
                    It.IsAny<Expression<Func<TodoEntity, bool>>>(),
                    true))
             .ReturnsAsync(3);

        var service = CreateService();

        Assert.Equal(3, await service.CountTodos(userId, true));
        _repo.Verify(r => r.CountTodosAsync(
            It.IsAny<Expression<Func<TodoEntity, bool>>>(), true), Times.Once);
    }

    [Fact]
    public async Task CountTodos_OnlyCompleted_FilterMatchesOnlyUsersCompletedTodos()
    {
        // Captures the WHERE expression the service builds and runs it against
        // sample todos - this pins the predicate itself (user AND completed),
        // which a mock-only pass-through test cannot catch.
        Guid userId = Guid.NewGuid();
        Expression<Func<TodoEntity, bool>>? captured = null;

        _repo.Setup(r => r.CountTodosAsync(It.IsAny<Expression<Func<TodoEntity, bool>>>(), true))
             .Callback<Expression<Func<TodoEntity, bool>>, bool>((f, _) => captured = f)
             .ReturnsAsync(0);

        var service = CreateService();
        await service.CountTodos(userId, true);

        Assert.NotNull(captured);
        Func<TodoEntity, bool> predicate = captured!.Compile();

        TodoEntity completedMine = SampleTodo(userId, Guid.NewGuid());
        completedMine.Status = Status.Completed;
        TodoEntity pendingMine = SampleTodo(userId, Guid.NewGuid());
        TodoEntity completedForeign = SampleTodo(Guid.NewGuid(), Guid.NewGuid());
        completedForeign.Status = Status.Completed;

        Assert.True(predicate(completedMine));
        Assert.False(predicate(pendingMine));
        Assert.False(predicate(completedForeign));
    }

    [Fact]
    public async Task GetTodosAfterDueDate_MapsGroupName()
    {
        Guid userId = Guid.NewGuid();
        TodoEntity todo = SampleTodo(userId, Guid.NewGuid());

        _repo.Setup(r => r.GetAllTodoNoTracking(
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
