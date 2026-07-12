using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApp.Areas.TodoMainApp.Controllers;
using TodoApp.Models;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Tests;

public class DashboardControllerTests
{
    private readonly Mock<ITodoService> _todoService = new();
    private readonly Mock<IGroupService> _groupService = new();
    private readonly Guid _userId = Guid.NewGuid();

    private DashboardController CreateController()
    {
        var controller = new DashboardController(_todoService.Object, _groupService.Object);

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
        _todoService.Setup(s => s.CountTodos(_userId, false)).ReturnsAsync(4);
        _todoService.Setup(s => s.CountTodos(_userId, true)).ReturnsAsync(2);
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
}
