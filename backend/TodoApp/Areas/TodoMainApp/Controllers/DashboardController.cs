using Microsoft.AspNetCore.Mvc;

using TodoApp.Models;
using TodoApp.Models.Todo;
using TodoApp.Services.Core;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

using static TodoApp.GCommon.ApplicationConstants.TodosSearchParameter;

namespace TodoApp.Areas.TodoMainApp.Controllers;

public class DashboardController: BaseController
{
    private readonly ITodoService _todoService;
    private readonly IGroupService _groupService;
    private readonly IRedisCacheService _redisCacheService;

    public DashboardController(
        ITodoService todoService,
        IGroupService groupService,
        IRedisCacheService redisCacheService)
    {
        _todoService = todoService;
        _groupService = groupService;
        _redisCacheService = redisCacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string redisKey = $"dashboard:{UserId}";
        
        IndexDashboardViewModel? cacheData = await _redisCacheService
            .GetData<IndexDashboardViewModel>(redisKey);
        
        if(cacheData != null)
            return View(cacheData);
        
        IEnumerable<TodosAfterDuelDate> todos = await _todoService
            .GetTodosAfterDueDate(UserId);

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
            .CountTodos(UserId, Pending);

        int countCompletedTodos = await _todoService
            .CountTodos(UserId, Completed);

        int countGroups = await _groupService.GetAllGroupsCount(UserId);

        IndexDashboardViewModel result = new IndexDashboardViewModel()
        {
            Todos = todoToDisplay,
            CountPendingTodos = countPendingTodos,
            CountCompletedTodos = countCompletedTodos,
            CountGroups = countGroups
        };

        await _redisCacheService.SetData(redisKey, result);

        return View(result);
    }
}