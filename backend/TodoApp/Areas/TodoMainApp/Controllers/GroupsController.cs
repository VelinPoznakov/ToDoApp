using Microsoft.AspNetCore.Mvc;
using TodoApp.Models.Group;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

namespace TodoApp.Areas.TodoMainApp.Controllers
{
    public class GroupsController : BaseController
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Guid userId = Guid.Parse(GetUserId()!);

            IEnumerable<GroupDto> groups = await _groupService
                .GetAllGroupsAsync(userId);

            IEnumerable<AllGroupsViewModel> result
                = groups.Select(g => new AllGroupsViewModel()
                {
                    Id = g.Id,
                    Name = g.Name
                });

            return View(result);
        }
    }
}
