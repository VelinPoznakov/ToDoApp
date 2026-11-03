using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApp.Areas.TodoMainApp.Controllers;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Todo;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;
using TodoApp.Web.ViewModels.Todo;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Tests.Todos;

// Controller tests mock the services and focus on ownership guards, status
// codes, redirect targets and which cache keys each action reads and clears.
public class TodoControllerTests
{
    private readonly Mock<ITodoService> _todoService = new();
    private readonly Mock<IGroupService> _groupService = new();
    private readonly Mock<IRedisCacheService> _redisCacheService = new();
    private readonly Guid _userId = Guid.NewGuid();

    private string DashboardKey => $"dashboard:{_userId}";

    public TodoControllerTests()
    {
        // The actions call keys.AddRange(...) so the mock must never return null.
        _redisCacheService
            .Setup(r => r.GetRedisKeysOnPatternAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new List<string>());
    }

    private TodoController CreateController()
    {
        var controller = new TodoController(
            _todoService.Object,
            _groupService.Object,
            Mock.Of<ILogger<TodoController>>(),
            _redisCacheService.Object);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) },
            "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    private static CreateEditViewModel ValidModel() => new CreateEditViewModel
    {
        Name = "Valid todo",
        Description = "Valid description",
        Priority = "High",
        DueDate = new DateOnly(2026, 12, 31)
    };

    private static AllTodoDto SampleDto() => new AllTodoDto
    {
        Id = Guid.NewGuid(),
        Name = "T1",
        Priority = "High",
        Status = "Pending",
        DueDate = "20-07-2026"
    };

    private static TodosAfterDuelDate SampleDtoWithGroup(string groupName) => new TodosAfterDuelDate
    {
        Id = Guid.NewGuid(),
        Name = "T1",
        Priority = "High",
        Status = "Pending",
        DueDate = "20-07-2026",
        GroupName = groupName,
        GroupId = Guid.NewGuid()
    };

    // Every filter returns its own key, so a test can prove exactly which
    // filters were cleared and that no filter's keys were overwritten.
    private void SetupKeysPerFilter()
    {
        foreach (TodosSearchParameter filter in Enum.GetValues<TodosSearchParameter>())
        {
            string name = filter.ToString();

            _redisCacheService
                .Setup(r => r.GetRedisKeysOnPatternAsync(
                    It.Is<string[]>(p => MatchesUserAndFilter(p, _userId.ToString(), name))))
                .ReturnsAsync(new List<string> { $"key-{name}" });
        }
    }

    private static bool MatchesUserAndFilter(string[] patterns, string userId, string filter)
        => patterns.Length == 2 && patterns[0] == userId && patterns[1] == filter;

    private void VerifyKeysDeleted(params string[] expected)
        => _redisCacheService.Verify(
            r => r.BulkDeleteAsync(It.Is<List<string>>(k => SameKeys(k, expected))),
            Times.Once);

    private static bool SameKeys(List<string> actual, string[] expected)
        => actual.OrderBy(k => k).SequenceEqual(expected.OrderBy(k => k));

    private void VerifyNothingCleared()
    {
        _redisCacheService.Verify(r => r.BulkDeleteAsync(It.IsAny<List<string>>()), Times.Never);
        _redisCacheService.Verify(r => r.RemoveData(It.IsAny<string>()), Times.Never);
    }

    // ===== Index =====

    [Fact]
    public async Task Index_ForeignGroup_ReturnsBadRequest_AndNeverLoadsTodos()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();
        IActionResult result = await controller.Index(groupId);

        Assert.IsType<BadRequestResult>(result);
        _todoService.Verify(
            s => s.GetTodosOrderByPriorityDueDateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<TodosSearchParameter>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_CacheHit_ReturnsCachedPage_AndNeverCallsTheService()
    {
        Guid groupId = Guid.NewGuid();
        string key = $"{TodosSearchParameter.Pending}:todos:{_userId}:{groupId}:2";
        PagedTodosViewModel cached = new PagedTodosViewModel { GroupName = "Cached" };

        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _redisCacheService.Setup(r => r.GetData<PagedTodosViewModel>(key)).ReturnsAsync(cached);

        var controller = CreateController();
        IActionResult result = await controller.Index(groupId, 2, TodosSearchParameter.Pending);

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Same(cached, view.Model);
        _todoService.Verify(
            s => s.GetTodosOrderByPriorityDueDateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<TodosSearchParameter>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_CacheMiss_MapsThePage_AndStoresItUnderFilterAndPageKey()
    {
        Guid groupId = Guid.NewGuid();
        string key = $"{TodosSearchParameter.Completed}:todos:{_userId}:{groupId}:1";

        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService
            .Setup(s => s.GetTodosOrderByPriorityDueDateAsync(_userId, groupId, 1, TodosSearchParameter.Completed))
            .ReturnsAsync(new PagedTodosDto<AllTodoDto>
            {
                Todos = new[] { SampleDto() },
                GroupName = "My Group",
                PageNumber = 1
            });
        _todoService
            .Setup(s => s.CountTodos(_userId, TodosSearchParameter.Completed, groupId))
            .ReturnsAsync(11);

        var controller = CreateController();
        IActionResult result = await controller.Index(groupId, 1, TodosSearchParameter.Completed);

        ViewResult view = Assert.IsType<ViewResult>(result);
        PagedTodosViewModel model = Assert.IsType<PagedTodosViewModel>(view.Model);
        Assert.Equal(groupId, model.GroupId);
        Assert.Equal("My Group", model.GroupName);
        Assert.Equal(2, model.PageNumber); // the view is 1-based, the query is 0-based
        Assert.Equal(11, model.TotalCount);
        Assert.Equal("T1", Assert.Single(model.Todos).Name);
        _redisCacheService.Verify(r => r.SetData(key, It.IsAny<PagedTodosViewModel>()), Times.Once);
    }

    [Fact]
    public async Task Index_NoTodos_IsNotCached()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService
            .Setup(s => s.GetTodosOrderByPriorityDueDateAsync(_userId, groupId, 0, TodosSearchParameter.All))
            .ReturnsAsync(new PagedTodosDto<AllTodoDto> { GroupName = string.Empty });
        _todoService.Setup(s => s.CountTodos(_userId, TodosSearchParameter.All, groupId)).ReturnsAsync(0);

        var controller = CreateController();

        Assert.IsType<ViewResult>(await controller.Index(groupId));
        _redisCacheService.Verify(
            r => r.SetData(It.IsAny<string>(), It.IsAny<PagedTodosViewModel>()),
            Times.Never);
    }

    // ===== Details =====

    [Fact]
    public async Task Details_CacheHit_ReturnsCachedModel_AndNeverCallsTheService()
    {
        Guid todoId = Guid.NewGuid();
        TodoDetailsViewmodel cached = new TodoDetailsViewmodel { Name = "Cached" };
        _redisCacheService
            .Setup(r => r.GetData<TodoDetailsViewmodel>($"todoDetails:{_userId}:{todoId}"))
            .ReturnsAsync(cached);

        var controller = CreateController();
        IActionResult result = await controller.Details(todoId);

        Assert.Same(cached, Assert.IsType<ViewResult>(result).Model);
        _todoService.Verify(
            s => s.GetTodoDetailsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Details_MissingTodo_ReturnsNotFound_AndCachesNothing()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.GetTodoDetailsAsync(_userId, todoId, false))
                    .ReturnsAsync((TodoDetailsDto?)null);

        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.Details(todoId));
        _redisCacheService.Verify(
            r => r.SetData(It.IsAny<string>(), It.IsAny<TodoDetailsViewmodel>()),
            Times.Never);
    }

    [Fact]
    public async Task Details_CacheMiss_MapsTheModel_AndStoresIt()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        _todoService.Setup(s => s.GetTodoDetailsAsync(_userId, todoId, false))
                    .ReturnsAsync(new TodoDetailsDto
                    {
                        Name = "T1",
                        Description = "D1",
                        Priority = "Low",
                        Status = "Pending",
                        DueDate = "20-07-2026",
                        CreatedOn = "01-07-2026",
                        GroupName = "G1",
                        GroupId = groupId,
                        Comments = new List<CommentDto> { new CommentDto { Id = 1, Content = "Nice" } }
                    });

        var controller = CreateController();
        IActionResult result = await controller.Details(todoId);

        TodoDetailsViewmodel model = Assert.IsType<TodoDetailsViewmodel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("T1", model.Name);
        Assert.Equal("G1", model.GroupName);
        Assert.Equal(groupId, model.GroupId);
        Assert.Single(model.Comments);
        _redisCacheService.Verify(
            r => r.SetData($"todoDetails:{_userId}:{todoId}", It.IsAny<TodoDetailsViewmodel>()),
            Times.Once);
    }

    // ===== Create =====

    [Fact]
    public async Task CreateGet_ForeignGroup_ReturnsBadRequest()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();

        Assert.IsType<BadRequestResult>(await controller.Create(groupId));
    }

    [Fact]
    public async Task CreatePost_ForeignGroup_ReturnsBadRequest_AndNeverAdds()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();

        Assert.IsType<BadRequestResult>(await controller.Create(groupId, ValidModel()));
        _todoService.Verify(
            s => s.AddTodoAsync(It.IsAny<CreateEditTodoDto>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePost_UnknownPriority_ReturnsViewAndNeverAdds()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);

        CreateEditViewModel model = ValidModel();
        model.Priority = "Urgent"; // not in the allowed Priorities list

        var controller = CreateController();
        IActionResult result = await controller.Create(groupId, model);

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.False(controller.ModelState.IsValid);
        _todoService.Verify(
            s => s.AddTodoAsync(It.IsAny<CreateEditTodoDto>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePost_Valid_AddsTodoForTheRouteGroup_AndRedirectsToIndex()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);

        CreateEditTodoDto? captured = null;
        _todoService.Setup(s => s.AddTodoAsync(It.IsAny<CreateEditTodoDto>(), _userId))
                    .Callback<CreateEditTodoDto, Guid>((dto, _) => captured = dto)
                    .Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.Create(groupId, ValidModel());

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
        Assert.NotNull(captured);
        Assert.Equal(groupId, captured!.GroupId);
        Assert.Equal("Valid todo", captured.Name);
    }

    [Fact]
    public async Task CreatePost_Valid_ClearsAllAndPendingPages_AndDashboard_ButNotCompleted()
    {
        // A new todo is always pending, so the Completed lists cannot change.
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        SetupKeysPerFilter();

        var controller = CreateController();
        await controller.Create(groupId, ValidModel());

        VerifyKeysDeleted("key-All", "key-Pending");
        _redisCacheService.Verify(r => r.RemoveData(DashboardKey), Times.Once);
    }

    [Fact]
    public async Task CreatePost_WhenPersistFails_ReturnsViewWithModelError_AndClearsNothing()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService.Setup(s => s.AddTodoAsync(It.IsAny<CreateEditTodoDto>(), _userId))
                    .ThrowsAsync(new DataPersistFail());

        var controller = CreateController();
        IActionResult result = await controller.Create(groupId, ValidModel());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        VerifyNothingCleared();
    }

    // ===== Edit =====

    [Fact]
    public async Task EditGet_MissingTodo_ReturnsNotFound()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.GetTodoDetailsAsync(_userId, todoId, true))
                    .ReturnsAsync((TodoDetailsDto?)null);

        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.Edit(todoId));
    }

    [Fact]
    public async Task EditGet_ExistingTodo_MapsTheFormModel()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.GetTodoDetailsAsync(_userId, todoId, true))
                    .ReturnsAsync(new TodoDetailsDto
                    {
                        Name = "T1",
                        Description = "D1",
                        Priority = "Low",
                        DueDate = "20-12-2026"
                    });

        var controller = CreateController();
        IActionResult result = await controller.Edit(todoId);

        CreateEditViewModel model = Assert.IsType<CreateEditViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("T1", model.Name);
        Assert.Equal("Low", model.Priority);
        Assert.Equal(new DateOnly(2026, 12, 20), model.DueDate);
    }

    [Fact]
    public async Task EditPost_MissingTodo_ReturnsBadRequest_AndClearsNothing()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.EditTodoAsync(_userId, todoId, It.IsAny<CreateEditTodoDto>()))
                    .ThrowsAsync(new EntityNotFoundException());

        var controller = CreateController();

        Assert.IsType<BadRequestResult>(await controller.Edit(todoId, ValidModel()));
        VerifyNothingCleared();
    }

    [Fact]
    public async Task EditPost_Valid_RedirectsToDetailsOfTheTodo()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.EditTodoAsync(_userId, todoId, It.IsAny<CreateEditTodoDto>()))
                    .Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.Edit(todoId, ValidModel());

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(todoId, redirect.RouteValues!["id"]);
    }

    [Fact]
    public async Task EditPost_Valid_ClearsEveryFilter_Details_AndDashboard()
    {
        // Regression: "keys = await ..." inside the loop kept only the last
        // filter's keys, which left the All tab stale after an edit.
        Guid todoId = Guid.NewGuid();
        SetupKeysPerFilter();

        var controller = CreateController();
        await controller.Edit(todoId, ValidModel());

        VerifyKeysDeleted("key-All", "key-Completed", "key-Pending");
        _redisCacheService.Verify(r => r.RemoveData($"todoDetails:{_userId}:{todoId}"), Times.Once);
        _redisCacheService.Verify(r => r.RemoveData(DashboardKey), Times.Once);
    }

    [Fact]
    public async Task EditPost_InvalidModel_ReturnsViewAndNeverEdits()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(CreateEditViewModel.Name), "required");

        CreateEditViewModel model = ValidModel();
        IActionResult result = await controller.Edit(Guid.NewGuid(), model);

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _todoService.Verify(
            s => s.EditTodoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CreateEditTodoDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EditPost_WhenPersistFails_ReturnsViewWithModelError_AndClearsNothing()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.EditTodoAsync(_userId, todoId, It.IsAny<CreateEditTodoDto>()))
                    .ThrowsAsync(new DataPersistFail());

        var controller = CreateController();
        IActionResult result = await controller.Edit(todoId, ValidModel());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        VerifyNothingCleared();
    }

    // ===== Delete =====

    [Fact]
    public async Task Delete_ForeignGroup_ReturnsBadRequest_AndNeverDeletes()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();
        IActionResult result = await controller.Delete(todoId, groupId);

        Assert.IsType<BadRequestResult>(result);
        _todoService.Verify(s => s.DeleteTodoAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        VerifyNothingCleared();
    }

    [Fact]
    public async Task Delete_OwnedGroup_Deletes_AndRedirectsToIndexWithGroupId()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);

        var controller = CreateController();
        IActionResult result = await controller.Delete(todoId, groupId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
        _todoService.Verify(s => s.DeleteTodoAsync(_userId, todoId), Times.Once);
    }

    [Fact]
    public async Task Delete_OwnedGroup_ClearsEveryFilter_Details_AndDashboard()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        SetupKeysPerFilter();

        var controller = CreateController();
        await controller.Delete(todoId, groupId);

        VerifyKeysDeleted("key-All", "key-Completed", "key-Pending");
        _redisCacheService.Verify(r => r.RemoveData($"todoDetails:{_userId}:{todoId}"), Times.Once);
        _redisCacheService.Verify(r => r.RemoveData(DashboardKey), Times.Once);
    }

    [Fact]
    public async Task Delete_MissingTodo_ReturnsNotFound_AndClearsNothing()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService.Setup(s => s.DeleteTodoAsync(_userId, todoId))
                    .ThrowsAsync(new EntityNotFoundException());

        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.Delete(todoId, groupId));
        VerifyNothingCleared();
    }

    // ===== Complete =====

    [Fact]
    public async Task Complete_MissingTodo_ReturnsNotFound_AndClearsNothing()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.CompleteTodo(todoId, _userId))
                    .ThrowsAsync(new EntityNotFoundException());

        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.Complete(todoId, Guid.NewGuid()));
        VerifyNothingCleared();
    }

    [Fact]
    public async Task Complete_Valid_RedirectsBackToTheSamePageAndTab()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        var controller = CreateController();
        IActionResult result = await controller.Complete(todoId, groupId, 3, TodosSearchParameter.Pending);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
        Assert.Equal(3, redirect.RouteValues!["page"]);
        Assert.Equal(TodosSearchParameter.Pending, redirect.RouteValues!["filter"]);
        _todoService.Verify(s => s.CompleteTodo(todoId, _userId), Times.Once);
    }

    [Theory]
    [InlineData(TodosSearchParameter.All)]
    [InlineData(TodosSearchParameter.Pending)]
    [InlineData(TodosSearchParameter.Completed)]
    public async Task Complete_FromAnyTab_ClearsEveryFilter_Details_AndDashboard(TodosSearchParameter tab)
    {
        // Regression: clearing only "the current tab + All" left the other
        // list stale, because a status change moves the todo between lists.
        Guid todoId = Guid.NewGuid();
        SetupKeysPerFilter();

        var controller = CreateController();
        await controller.Complete(todoId, Guid.NewGuid(), 0, tab);

        VerifyKeysDeleted("key-All", "key-Completed", "key-Pending");
        _redisCacheService.Verify(r => r.RemoveData($"todoDetails:{_userId}:{todoId}"), Times.Once);
        _redisCacheService.Verify(r => r.RemoveData(DashboardKey), Times.Once);
    }

    [Fact]
    public async Task Complete_WhenPersistFails_RedirectsBack_AndClearsNothing()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.CompleteTodo(todoId, _userId)).ThrowsAsync(new DataPersistFail());

        var controller = CreateController();

        Assert.IsType<RedirectToActionResult>(await controller.Complete(todoId, Guid.NewGuid()));
        VerifyNothingCleared();
    }

    // ===== Activate =====

    [Fact]
    public async Task Activate_MissingTodo_ReturnsNotFound_AndClearsNothing()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.ActivateTodoAsync(_userId, todoId))
                    .ThrowsAsync(new EntityNotFoundException());

        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.Activate(todoId, Guid.NewGuid()));
        VerifyNothingCleared();
    }

    [Fact]
    public async Task Activate_Valid_RedirectsBackToTheSamePageAndTab()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        var controller = CreateController();
        IActionResult result = await controller.Activate(todoId, groupId, 2, TodosSearchParameter.Completed);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
        Assert.Equal(2, redirect.RouteValues!["page"]);
        Assert.Equal(TodosSearchParameter.Completed, redirect.RouteValues!["filter"]);
        _todoService.Verify(s => s.ActivateTodoAsync(_userId, todoId), Times.Once);
    }

    [Theory]
    [InlineData(TodosSearchParameter.All)]
    [InlineData(TodosSearchParameter.Pending)]
    [InlineData(TodosSearchParameter.Completed)]
    public async Task Activate_FromAnyTab_ClearsEveryFilter_Details_AndDashboard(TodosSearchParameter tab)
    {
        Guid todoId = Guid.NewGuid();
        SetupKeysPerFilter();

        var controller = CreateController();
        await controller.Activate(todoId, Guid.NewGuid(), 0, tab);

        VerifyKeysDeleted("key-All", "key-Completed", "key-Pending");
        _redisCacheService.Verify(r => r.RemoveData($"todoDetails:{_userId}:{todoId}"), Times.Once);
        _redisCacheService.Verify(r => r.RemoveData(DashboardKey), Times.Once);
    }

    // ===== TodosByStatus (dashboard cards) =====

    [Theory]
    [InlineData(TodosSearchParameter.All)]
    [InlineData((TodosSearchParameter)5)]
    public async Task TodosByStatus_NotPendingOrCompleted_ReturnsNotFound(TodosSearchParameter filter)
    {
        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.TodosByStatus(filter));
        _todoService.Verify(
            s => s.GetTodosFromAllGroups(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<TodosSearchParameter>()),
            Times.Never);
    }

    [Fact]
    public async Task TodosByStatus_CacheHit_ReturnsCachedPage_AndNeverCallsTheService()
    {
        string key = $"All{TodosSearchParameter.Pending}:todos:{_userId}:1";
        PagedTodosViewModel cached = new PagedTodosViewModel();
        _redisCacheService.Setup(r => r.GetData<PagedTodosViewModel>(key)).ReturnsAsync(cached);

        var controller = CreateController();
        IActionResult result = await controller.TodosByStatus(TodosSearchParameter.Pending, 1);

        Assert.Same(cached, Assert.IsType<ViewResult>(result).Model);
        _todoService.Verify(
            s => s.GetTodosFromAllGroups(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<TodosSearchParameter>()),
            Times.Never);
    }

    [Theory]
    [InlineData(TodosSearchParameter.Pending)]
    [InlineData(TodosSearchParameter.Completed)]
    public async Task TodosByStatus_CacheMiss_MapsRowsWithTheirGroup_AndStoresThePage(TodosSearchParameter filter)
    {
        string key = $"All{filter}:todos:{_userId}:0";

        _todoService.Setup(s => s.GetTodosFromAllGroups(0, _userId, filter))
                    .ReturnsAsync(new PagedTodosDto<TodosAfterDuelDate>
                    {
                        Todos = new[] { SampleDtoWithGroup("Work") },
                        GroupName = "Work",
                        PageNumber = 0
                    });
        _todoService.Setup(s => s.CountTodos(_userId, filter, null)).ReturnsAsync(1);

        var controller = CreateController();
        IActionResult result = await controller.TodosByStatus(filter);

        PagedTodosViewModel model = Assert.IsType<PagedTodosViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        TodoWithGroupNameViewModel row = Assert.IsType<TodoWithGroupNameViewModel>(Assert.Single(model.Todos));
        Assert.Equal("Work", row.GroupName);
        Assert.Equal(1, model.PageNumber);
        Assert.Equal(1, model.TotalCount);
        _redisCacheService.Verify(r => r.SetData(key, It.IsAny<PagedTodosViewModel>()), Times.Once);
    }

    [Fact]
    public async Task TodosByStatus_NoTodos_IsNotCached()
    {
        _todoService.Setup(s => s.GetTodosFromAllGroups(0, _userId, TodosSearchParameter.Pending))
                    .ReturnsAsync(new PagedTodosDto<TodosAfterDuelDate> { GroupName = string.Empty });
        _todoService.Setup(s => s.CountTodos(_userId, TodosSearchParameter.Pending, null)).ReturnsAsync(0);

        var controller = CreateController();

        Assert.IsType<ViewResult>(await controller.TodosByStatus(TodosSearchParameter.Pending));
        _redisCacheService.Verify(
            r => r.SetData(It.IsAny<string>(), It.IsAny<PagedTodosViewModel>()),
            Times.Never);
    }
}
