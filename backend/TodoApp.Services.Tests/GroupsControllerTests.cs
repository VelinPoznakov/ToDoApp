using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApp.Areas.TodoMainApp.Controllers;
using TodoApp.Models.Group;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Tests;

// Controller tests mock IGroupService and focus on authorization guards,
// status codes and redirect targets.
public class GroupsControllerTests
{
    private readonly Mock<IGroupService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();

    private GroupsController CreateController()
    {
        var controller = new GroupsController(
            _service.Object,
            Mock.Of<ILogger<GroupsController>>());

        // Authenticated user for BaseController.GetUserId()
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

    // ===== DeleteGroup =====

    [Fact]
    public async Task DeleteGroup_ForeignGroup_ReturnsBadRequest_AndNeverDeletes()
    {
        Guid groupId = Guid.NewGuid();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();
        IActionResult result = await controller.DeleteGroup(groupId);

        Assert.IsType<BadRequestResult>(result);
        _service.Verify(s => s.DeleteGroupAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroup_OwnedGroup_Deletes_AndRedirectsToGroupsIndex()
    {
        Guid groupId = Guid.NewGuid();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _service.Setup(s => s.DeleteGroupAsync(groupId)).Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.DeleteGroup(groupId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        _service.Verify(s => s.DeleteGroupAsync(groupId), Times.Once);
    }

    // ===== CreateGroup =====

    [Fact]
    public async Task CreateGroupPost_InvalidModel_ReturnsViewWithSameModel()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(CreateEditGroupViewModel.GroupName), "required");

        var model = new CreateEditGroupViewModel { GroupName = "" };
        IActionResult result = await controller.CreateGroup(model);

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _service.Verify(
            s => s.CreateGroup(It.IsAny<CreateEditGroupDto>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateGroupPost_Valid_RedirectsToTodoIndexOfNewGroup()
    {
        Guid newGroupId = Guid.NewGuid();
        _service.Setup(s => s.CreateGroup(It.IsAny<CreateEditGroupDto>(), _userId))
                .ReturnsAsync(newGroupId);

        var controller = CreateController();
        IActionResult result = await controller.CreateGroup(
            new CreateEditGroupViewModel { GroupName = "Fresh" });

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Todo", redirect.ControllerName);
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal(newGroupId, redirect.RouteValues!["id"]);
    }

    // ===== EditGroup GET =====

    [Fact]
    public async Task EditGroupGet_ChecksOwnership_BeforeExposingGroupData()
    {
        // SECURITY (IDOR): a user must not be able to open the edit form
        // of a group that belongs to someone else.
        Guid foreignGroupId = Guid.NewGuid();

        _service.Setup(s => s.GroupExistsAsync(foreignGroupId, _userId)).ReturnsAsync(false);
        _service.Setup(s => s.GetGroupNameForEditTracked(foreignGroupId))
                .ReturnsAsync(new CreateEditGroupDto { GroupName = "SomeoneElsesSecretList" });

        var controller = CreateController();
        IActionResult result = await controller.EditGroup(foreignGroupId);

        // The foreign group's data must NOT be handed to the view.
        Assert.False(result is ViewResult,
            "Edit GET returned the group's data without an ownership check (IDOR).");
    }

    // ===== EditGroup POST =====

    [Fact]
    public async Task EditGroupPost_ForeignGroup_ReturnsNotFound_AndNeverEdits()
    {
        Guid groupId = Guid.NewGuid();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();
        IActionResult result = await controller.EditGroup(
            new CreateEditGroupViewModel { GroupName = "Hack" }, groupId);

        Assert.IsType<NotFoundResult>(result);
        _service.Verify(
            s => s.EditGroup(It.IsAny<Guid>(), It.IsAny<CreateEditGroupDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EditGroupPost_Valid_RedirectsToTodoIndexWithGroupId()
    {
        Guid groupId = Guid.NewGuid();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);
        _service.Setup(s => s.EditGroup(groupId, It.IsAny<CreateEditGroupDto>()))
                .Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.EditGroup(
            new CreateEditGroupViewModel { GroupName = "Renamed" }, groupId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Todo", redirect.ControllerName);
        // The redirect must carry the group id, otherwise Todo/Index gets
        // Guid.Empty and returns BadRequest right after a successful save.
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal(groupId, redirect.RouteValues!["id"]);
    }

    [Fact]
    public async Task EditGroupPost_InvalidModel_ReturnsViewWithSameModel()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(CreateEditGroupViewModel.GroupName), "too short");

        var model = new CreateEditGroupViewModel { GroupName = "x" };
        IActionResult result = await controller.EditGroup(model, Guid.NewGuid());

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _service.Verify(
            s => s.EditGroup(It.IsAny<Guid>(), It.IsAny<CreateEditGroupDto>()),
            Times.Never);
    }
}
