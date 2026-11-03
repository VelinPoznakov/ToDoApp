using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApp.Areas.TodoMainApp.Controllers;
using TodoApp.Models.Group;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Tests.Groups;

// Controller tests mock IGroupService and focus on authorization guards,
// status codes and redirect targets.
public class GroupsControllerTests
{
    private readonly Mock<IGroupService> _service = new();
    private readonly Mock<IRedisCacheService> _redisCacheService = new();
    private readonly Guid _userId = Guid.NewGuid();

    private GroupsController CreateController()
    {
        var controller = new GroupsController(
            _service.Object,
            Mock.Of<ILogger<GroupsController>>(),
            _redisCacheService.Object);

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

    // ===== Index (cache) =====

    [Fact]
    public async Task Index_CacheHit_ReturnsCachedGroups_AndNeverCallsTheService()
    {
        // Regression: "View(result);" without return ignored the cached value,
        // so every request still went to the database.
        IEnumerable<AllGroupsViewModel> cached = new[] { new AllGroupsViewModel { Name = "Cached" } };
        _redisCacheService
            .Setup(r => r.GetData<IEnumerable<AllGroupsViewModel>>($"groups:{_userId}0"))
            .ReturnsAsync(cached);

        var controller = CreateController();
        IActionResult result = await controller.Index();

        Assert.Same(cached, Assert.IsType<ViewResult>(result).Model);
        _service.Verify(s => s.GetAllGroupsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Index_CacheMiss_MapsGroups_AndStoresThem()
    {
        string key = $"groups:{_userId}0";
        _redisCacheService
            .Setup(r => r.GetData<IEnumerable<AllGroupsViewModel>>(key))
            .ReturnsAsync((IEnumerable<AllGroupsViewModel>?)null);
        _service.Setup(s => s.GetAllGroupsAsync(_userId))
                .ReturnsAsync(new[] { new GroupDto { Id = Guid.NewGuid(), Name = "Work" } });

        var controller = CreateController();
        IActionResult result = await controller.Index();

        var model = Assert.IsAssignableFrom<IEnumerable<AllGroupsViewModel>>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Work", Assert.Single(model).Name);
        _redisCacheService.Verify(
            r => r.SetData(key, It.IsAny<IEnumerable<AllGroupsViewModel>>()),
            Times.Once);
    }

    // ===== Cache clearing =====

    private List<string> SetupUserKeys()
    {
        List<string> keys = new List<string> { "key-1", "key-2" };

        _redisCacheService
            .Setup(r => r.GetRedisKeysOnPatternAsync(
                It.Is<string[]>(p => p.Length == 1 && p[0] == _userId.ToString())))
            .ReturnsAsync(keys);

        return keys;
    }

    [Fact]
    public async Task DeleteGroup_OwnedGroup_ClearsAllOfTheUsersKeys()
    {
        Guid groupId = Guid.NewGuid();
        List<string> keys = SetupUserKeys();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);

        var controller = CreateController();
        await controller.DeleteGroup(groupId);

        _redisCacheService.Verify(r => r.BulkDeleteAsync(keys), Times.Once);
    }

    [Fact]
    public async Task DeleteGroup_ForeignGroup_ClearsNothing()
    {
        Guid groupId = Guid.NewGuid();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();
        await controller.DeleteGroup(groupId);

        _redisCacheService.Verify(r => r.BulkDeleteAsync(It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task CreateGroup_Valid_ClearsAllOfTheUsersKeys()
    {
        List<string> keys = SetupUserKeys();
        _service.Setup(s => s.CreateGroup(It.IsAny<CreateEditGroupDto>(), _userId))
                .ReturnsAsync(Guid.NewGuid());

        var controller = CreateController();
        await controller.CreateGroup(new CreateEditGroupViewModel { GroupName = "Fresh" });

        _redisCacheService.Verify(r => r.BulkDeleteAsync(keys), Times.Once);
    }

    [Fact]
    public async Task EditGroup_Valid_ClearsAllOfTheUsersKeys()
    {
        // The group name is stored inside the cached todo pages and details.
        Guid groupId = Guid.NewGuid();
        List<string> keys = SetupUserKeys();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(true);

        var controller = CreateController();
        await controller.EditGroup(new CreateEditGroupViewModel { GroupName = "Renamed" }, groupId);

        _redisCacheService.Verify(r => r.BulkDeleteAsync(keys), Times.Once);
    }

    [Fact]
    public async Task EditGroup_ForeignGroup_ClearsNothing()
    {
        Guid groupId = Guid.NewGuid();
        _service.Setup(s => s.GroupExistsAsync(groupId, _userId)).ReturnsAsync(false);

        var controller = CreateController();
        await controller.EditGroup(new CreateEditGroupViewModel { GroupName = "Hack" }, groupId);

        _redisCacheService.Verify(r => r.BulkDeleteAsync(It.IsAny<List<string>>()), Times.Never);
    }
}
