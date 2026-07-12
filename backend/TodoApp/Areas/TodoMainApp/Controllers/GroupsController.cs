using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Group;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

using static TodoApp.GCommon.ErrorMessages;


namespace TodoApp.Areas.TodoMainApp.Controllers
{
    public class GroupsController : BaseController
    {
        private readonly IGroupService _groupService;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(IGroupService groupService, ILogger<GroupsController> logger)
        {
            _groupService = groupService;
            _logger = logger;
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

        [HttpPost]
        public async Task<IActionResult> DeleteGroup([FromRoute]Guid id)
        {
            if (string.IsNullOrEmpty(id.ToString()))
            {
                return BadRequest();
            }

            Guid userId = Guid.Parse(GetUserId()!);

            bool doesItBelongToTheUserAndExists = await _groupService.GroupExistsAsync(id, userId);

            if (!doesItBelongToTheUserAndExists)
            {
                return BadRequest();
            }

            try
            {
                await _groupService.DeleteGroupAsync(id);
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
            catch (DataPersistFail e)
            {
                _logger.LogError(e, DataPersistFailErrorMessage);

                return RedirectToAction(nameof(Index), "Todo", new { id = id });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult CreateGroup()
        {
            return View(new CreateEditGroupViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromForm] CreateEditGroupViewModel formData)
        {
            Guid userId = Guid.Parse(GetUserId()!);

            if (!ModelState.IsValid)
            {
                return View(formData);
            }

            CreateEditGroupDto newGroup = new CreateEditGroupDto()
            {
                GroupName = formData.GroupName
            };

            Guid? groupId = null;

            try
            {
                groupId = await _groupService.CreateGroup(newGroup, userId);
            }
            catch (DataPersistFail e)
            {
                ModelState.AddModelError(string.Empty, DataPersistFailErrorMessage);
                _logger.LogError(e, string.Format(DataPersistFailErrorMessage, nameof(CreateGroup)));

                return View(formData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, UnexpectedErrorMessage);
                ModelState.AddModelError(string.Empty, UnexpectedErrorMessage);

                return View(formData);
            }

            return RedirectToAction(nameof(Index), "Todo", new { id = groupId });
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup([FromRoute]Guid id)
        {
            if (string.IsNullOrWhiteSpace(id.ToString()))
            {
                return BadRequest();
            }

            try
            {
                CreateEditGroupDto group = await _groupService.GetGroupNameForEditTracked(id);

                CreateEditGroupViewModel groupViewModel = new CreateEditGroupViewModel()
                {
                    GroupName = group.GroupName
                };

                return View(groupViewModel);
            }
            catch (EntityNotFoundException)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup([FromForm] CreateEditGroupViewModel model, [FromRoute] Guid id)
        {
            if (string.IsNullOrEmpty(id.ToString()))
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid userId = Guid.Parse(GetUserId()!);

            bool isGroupExistingAndBelongToTheUser = await _groupService.GroupExistsAsync(id, userId);

            if (!isGroupExistingAndBelongToTheUser)
            {
                return NotFound();
            }

            CreateEditGroupDto group = new CreateEditGroupDto()
            {
                GroupName = model.GroupName
            };

            try
            {
                await _groupService.EditGroup(id, group);

                return RedirectToAction(nameof(Index), "Todo", new[] {id = id});
            }
            catch (DataPersistFail e)
            {
                ModelState.AddModelError(string.Empty, DataPersistFailErrorMessage);
                _logger.LogError(e, string.Format(DataPersistFailErrorMessage, nameof(EditGroup)));

                return View(model);
            }
        }
    }
}
