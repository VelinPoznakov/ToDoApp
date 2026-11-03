using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApp.Areas.TodoMainApp.Controllers;
using TodoApp.Models;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Tests.Dashboard;

public class DashboardControllerTests
{
    private readonly Mock<ITodoService> _todoService = new();
    private readonly Mock<IGroupService> _groupService = new();
    private readonly Mock<IRedisCacheService> _redisCacheService = new();
    private readonly Guid _userId = Guid.NewGuid();

    private DashboardController CreateController()
    {
        var controller = new DashboardController(
            _todoService.Object,
            _groupService.Object,
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

    [Fact]
    public async Task Index_MapsOverdueTodosAndAllThreeCounters()
    {
        _todoService.Setup(s => s.GetTodosAfterDueDate(_userId))
                    .ReturnsAsync(new[]
                    {
                        new TodosAfterDuelDate
                        {
                            Id = Guid.NewGuid(),
                            Name = "Overdue",
                            Priority = "High",
                            Status = "Pending",
                            DueDate = "01-07-2026",
                            GroupName = "Work"
                        }
                    });
        _todoService.Setup(s => s.CountTodos(_userId, TodosSearchParameter.Pending, null)).ReturnsAsync(4);
        _todoService.Setup(s => s.CountTodos(_userId, TodosSearchParameter.Completed, null)).ReturnsAsync(2);
        _groupService.Setup(s => s.GetAllGroupsCount(_userId)).ReturnsAsync(3);

        var controller = CreateController();
        IActionResult result = await controller.Index();

        ViewResult view = Assert.IsType<ViewResult>(result);
        IndexDashboardViewModel model = Assert.IsType<IndexDashboardViewModel>(view.Model);

        Assert.Single(model.Todos);
        Assert.Equal("Work", model.Todos.First().GroupName);
        Assert.Equal(4, model.CountPendingTodos);
        Assert.Equal(2, model.CountCompletedTodos);
        Assert.Equal(3, model.CountGroups);
    }

    [Fact]
    public async Task Index_CacheHit_ReturnsCachedModel_AndNeverCallsTheServices()
    {
        IndexDashboardViewModel cached = new IndexDashboardViewModel { CountGroups = 9 };
        _redisCacheService
            .Setup(r => r.GetData<IndexDashboardViewModel>($"dashboard:{_userId}"))
            .ReturnsAsync(cached);

        var controller = CreateController();
        IActionResult result = await controller.Index();

        Assert.Same(cached, Assert.IsType<ViewResult>(result).Model);
        _todoService.Verify(s => s.GetTodosAfterDueDate(It.IsAny<Guid>()), Times.Never);
        _groupService.Verify(s => s.GetAllGroupsCount(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Index_CacheMiss_StoresTheModelUnderTheUsersKey()
    {
        _todoService.Setup(s => s.GetTodosAfterDueDate(_userId))
                    .ReturnsAsync(Array.Empty<TodosAfterDuelDate>());

        var controller = CreateController();
        await controller.Index();

        _redisCacheService.Verify(
            r => r.SetData($"dashboard:{_userId}", It.IsAny<IndexDashboardViewModel>()),
            Times.Once);
    }
}
