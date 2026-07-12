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

namespace TodoApp.Services.Tests;

// Controller tests mock ITodoService/IGroupService and focus on the
// ownership guards, status codes and redirect targets of TodoController.
public class TodoControllerTests
{
    private readonly Mock<ITodoService> _todoService = new();
    private readonly Mock<IGroupService> _groupService = new();
    private readonly Guid _userId = Guid.NewGuid();

    private TodoController CreateController()
    {
        var controller = new TodoController(
            _todoService.Object,
            _groupService.Object,
            Mock.Of<ILogger<TodoController>>());

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
            s => s.GetAllTodosOrderByPriorityDueDateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_OwnedGroup_ReturnsViewWithMappedTodos()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService.Setup(s => s.GetAllTodosOrderByPriorityDueDateAsync(_userId, groupId, false, false))
                    .ReturnsAsync(new[]
                    {
                        new AllTodoDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "T1",
                            Priority = "High",
                            Status = "Pending",
                            DueDate = "20-07-2026"
                        }
                    });

        var controller = CreateController();
        IActionResult result = await controller.Index(groupId);

        ViewResult view = Assert.IsType<ViewResult>(result);
        AllTodosViewModel model = Assert.IsType<AllTodosViewModel>(view.Model);
        Assert.Equal(groupId, model.GroupId);
        TodoViewModel todo = Assert.Single(model.Todos);
        Assert.Equal("T1", todo.Name);
        Assert.Equal("High", todo.Priority);
    }

    // ===== Details =====

    [Fact]
    public async Task Details_MissingTodo_ReturnsNotFound()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.GetTodoDetailsAsync(_userId, todoId, false))
                    .ReturnsAsync((TodoDetailsDto?)null);

        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.Details(todoId));
    }

    [Fact]
    public async Task Details_ExistingTodo_ReturnsViewWithMappedModel()
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

        ViewResult view = Assert.IsType<ViewResult>(result);
        TodoDetailsViewmodel model = Assert.IsType<TodoDetailsViewmodel>(view.Model);
        Assert.Equal("T1", model.Name);
        Assert.Equal("G1", model.GroupName);
        Assert.Equal(groupId, model.GroupId);
        Assert.Single(model.Comments);
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
    public async Task CreatePost_WhenPersistFails_ReturnsViewWithModelError()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService.Setup(s => s.AddTodoAsync(It.IsAny<CreateEditTodoDto>(), _userId))
                    .ThrowsAsync(new DataPersistFail());

        var controller = CreateController();
        IActionResult result = await controller.Create(groupId, ValidModel());

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    // ===== Edit =====

    [Fact]
    public async Task EditPost_MissingTodo_ReturnsBadRequest()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.EditTodoAsync(_userId, todoId, It.IsAny<CreateEditTodoDto>()))
                    .ThrowsAsync(new EntityNotFoundException());

        var controller = CreateController();

        Assert.IsType<BadRequestResult>(await controller.Edit(todoId, ValidModel()));
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
        _todoService.Verify(
            s => s.EditTodoAsync(_userId, todoId, It.IsAny<CreateEditTodoDto>()),
            Times.Once);
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
    }

    [Fact]
    public async Task Delete_OwnedGroup_Deletes_AndRedirectsToIndexWithGroupId()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService.Setup(s => s.DeleteTodoAsync(_userId, todoId)).Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.Delete(todoId, groupId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
        _todoService.Verify(s => s.DeleteTodoAsync(_userId, todoId), Times.Once);
    }

    // ===== Completed list =====

    [Fact]
    public async Task Completed_OwnedGroup_RequestsCompletedTodosIgnoringQueryFilter()
    {
        Guid groupId = Guid.NewGuid();
        _groupService.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _todoService.Setup(s => s.GetAllTodosOrderByPriorityDueDateAsync(_userId, groupId, true, true))
                    .ReturnsAsync(Array.Empty<AllTodoDto>());

        var controller = CreateController();
        IActionResult result = await controller.Completed(groupId);

        Assert.IsType<ViewResult>(result);
        _todoService.Verify(
            s => s.GetAllTodosOrderByPriorityDueDateAsync(_userId, groupId, true, true),
            Times.Once);
    }

    // ===== Complete / Activate =====

    [Fact]
    public async Task Complete_MissingTodo_ReturnsNotFound()
    {
        Guid todoId = Guid.NewGuid();
        _todoService.Setup(s => s.CompleteTodo(todoId, _userId))
                    .ThrowsAsync(new EntityNotFoundException());

        var controller = CreateController();

        Assert.IsType<NotFoundResult>(await controller.Complete(todoId, Guid.NewGuid()));
    }

    [Fact]
    public async Task Complete_Valid_RedirectsToCompletedWithGroupId()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        _todoService.Setup(s => s.CompleteTodo(todoId, _userId)).Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.Complete(todoId, groupId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Completed", redirect.ActionName);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
    }

    [Fact]
    public async Task Activate_Valid_RedirectsToIndexWithGroupId()
    {
        Guid todoId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        _todoService.Setup(s => s.ActivateTodoAsync(_userId, todoId)).Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.Activate(todoId, groupId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
        _todoService.Verify(s => s.ActivateTodoAsync(_userId, todoId), Times.Once);
    }
}
