using Microsoft.AspNetCore.Mvc;
using TodoApp.Models;
using TodoApp.Models.Todo;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

namespace TodoApp.Areas.TodoMainApp.Controllers;

public class DashboardController: BaseController
{
    private readonly ITodoService _todoService;
    private readonly IGroupService _groupService;

    public DashboardController(ITodoService todoService, IGroupService groupService)
    {
        _todoService = todoService;
        _groupService = groupService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Guid userId = Guid.Parse(GetUserId()!);

        IEnumerable<TodosAfterDuelDate> todos = await _todoService
            .GetTodosAfterDueDate(userId);

        IEnumerable<TodoWithGroupNameViewModel> todoToDisplay = 
            todos.Select(t => new TodoWithGroupNameViewModel()
            {
                Id = t.Id,
                Name = t.Name,
                Priority = t.Priority,
                Status = t.Status,
                DueDate = t.DueDate,
                GroupName = t.GroupName,
                GroupId = t.GroupId
            });

        int countPendingTodos = await _todoService
            .CountTodos(userId);

        int countCompletedTodos = await _todoService
            .CountTodos(userId, true);

        int countGroups = await _groupService.GetAllGroupsCount(userId);

        IndexDashboardViewModel result = new IndexDashboardViewModel()
        {
            Todos = todoToDisplay,
            CountPendingTodos = countPendingTodos,
            CountCompletedTodos = countCompletedTodos,
            CountGroups = countGroups
        };

        return View(result);
    }
}