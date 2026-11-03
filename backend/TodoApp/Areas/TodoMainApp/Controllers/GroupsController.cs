using Microsoft.AspNetCore.Mvc;

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
        private readonly IRedisCacheService _redisCacheService;

        public GroupsController(
            IGroupService groupService,
            ILogger<GroupsController> logger,
            IRedisCacheService redisCacheService)
        {
            _groupService = groupService;
            _logger = logger;
            _redisCacheService = redisCacheService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int page = 0)
        {
            string redisKey = $"groups:{UserId}{page}";
            
            IEnumerable<AllGroupsViewModel>? result = await _redisCacheService
                .GetData<IEnumerable<AllGroupsViewModel>>(redisKey);
            
            if(result != null)
                return View(result);
            
            IEnumerable<GroupDto> groups = await _groupService
                .GetAllGroupsAsync(UserId);

            result = groups.Select(g => new AllGroupsViewModel()
            {
                Id = g.Id,
                Name = g.Name
            });
            
            await _redisCacheService.SetData(redisKey, result);

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup([FromRoute]Guid id)
        {
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest();

            bool doesItBelongToTheUserAndExists = await _groupService.GroupExistsAsync(id, UserId);

            if (!doesItBelongToTheUserAndExists)
                return BadRequest();

            try
            {
                await _groupService.DeleteGroupAsync(id);

                List<string> redisKeys = await _redisCacheService
                    .GetRedisKeysOnPatternAsync(UserId.ToString());
                
                await _redisCacheService.BulkDeleteAsync(redisKeys);
                
                await _redisCacheService.RemoveData($"dashboard:{UserId}");
                
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
            if (!ModelState.IsValid)
                return View(formData);

            CreateEditGroupDto newGroup = new CreateEditGroupDto()
            {
                GroupName = formData.GroupName
            };

            Guid? groupId = null;

            try
            {
                groupId = await _groupService.CreateGroup(newGroup, UserId);
                
                List<string> redisKeys = await _redisCacheService
                    .GetRedisKeysOnPatternAsync(UserId.ToString());
                
                await _redisCacheService.BulkDeleteAsync(redisKeys);
                await _redisCacheService.RemoveData($"dashboard:{UserId}");

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
                return BadRequest();
            
            bool isGroupOwner = await _groupService.GroupExistsAsync(id, UserId);

            if (!isGroupOwner)
                return NotFound();

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
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            bool isGroupExistingAndBelongToTheUser = await _groupService.GroupExistsAsync(id, UserId);

            if (!isGroupExistingAndBelongToTheUser)
                return NotFound();

            CreateEditGroupDto group = new CreateEditGroupDto()
            {
                GroupName = model.GroupName
            };

            try
            {
                await _groupService.EditGroup(id, group);
                
                List<string> redisKeys = await _redisCacheService
                    .GetRedisKeysOnPatternAsync(UserId.ToString());
                
                await _redisCacheService.BulkDeleteAsync(redisKeys);
                await _redisCacheService.RemoveData($"dashboard:{UserId}");

                return RedirectToAction(nameof(Index), "Todo", new { id = id });
            }
            catch (EntityNotFoundException)
            {
                return BadRequest();
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
