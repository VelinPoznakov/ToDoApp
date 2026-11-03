using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Comment;
using TodoApp.Models.Todo;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;
using TodoApp.Web.ViewModels.Todo;

using static TodoApp.GCommon.ModelsErrorMessages;
using static TodoApp.GCommon.ErrorMessages;
using static TodoApp.GCommon.ApplicationConstants;
using static TodoApp.GCommon.ApplicationConstants.TodosSearchParameter;

namespace TodoApp.Areas.TodoMainApp.Controllers
{
    public class TodoController : BaseController
    {
        private readonly ITodoService _todoService;
        private readonly IGroupService _groupService;
        private readonly ILogger<TodoController> _logger;
        private readonly IRedisCacheService _redisCacheService;
        
        public TodoController(
            ITodoService todoService,
            IGroupService groupService,
            ILogger<TodoController> logger,
            IRedisCacheService redisCacheService)
        {
            _todoService = todoService;
            _groupService = groupService;
            _logger = logger;
            _redisCacheService = redisCacheService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromRoute] Guid id,
            [FromQuery] int page = 0,
            [FromQuery] TodosSearchParameter filter = All
            )
        {
            if (id.ToString().IsNullOrEmpty())
                return BadRequest();

            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(id, UserId);

            if (!groupExistsAndBelongToUser)
                return BadRequest();

            string redisKey = $"{filter}:todos:{UserId}:{id}:{page}";
            
            PagedTodosViewModel? result = await _redisCacheService
                .GetData<PagedTodosViewModel>(redisKey);

            if (result != null)
                return View(result);
            
            PagedTodosDto<AllTodoDto> allTodos = await _todoService
                .GetTodosOrderByPriorityDueDateAsync(UserId, id, page, filter);
            
            int countTodos = await _todoService
                .CountTodos(UserId, filter, id);
            
            IEnumerable<TodoViewModel> todos = allTodos.Todos.Select(t => new TodoViewModel()
            {
                Id = t.Id,
                Name = t.Name,
                DueDate = t.DueDate,
                Priority = t.Priority,
                Status = t.Status
            });

            result = new PagedTodosViewModel()
            {
                Todos = todos,
                GroupId = id,
                GroupName = allTodos.GroupName,
                PageNumber = page + 1,
                TotalCount = countTodos
            };
            
            if(result.TotalCount > 0)
                await _redisCacheService.SetData(redisKey, result);

            return View(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] Guid id)
        {
            if (id.ToString().IsNullOrEmpty())
                return BadRequest();
            
            string redisKey = $"todoDetails:{UserId}:{id}";
            
            TodoDetailsViewmodel? result = await _redisCacheService
                .GetData<TodoDetailsViewmodel>(redisKey);

            if (result != null)
                return View(result);

            TodoDetailsDto? todo = await _todoService
                .GetTodoDetailsAsync(UserId, id);

            if (todo == null)
                return NotFound();

            result = new TodoDetailsViewmodel()
            {
                Name = todo.Name,
                Description = todo.Description,
                Priority = todo.Priority,
                Status = todo.Status,
                DueDate = todo.DueDate,
                GroupName = todo.GroupName,
                CreatedOn = todo.CreatedOn,
                GroupId = todo.GroupId,
                Comments = todo.Comments.Select(c => new CommentViewModel()
                {
                    Id = c.Id,
                    Content = c.Content,
                }).ToList()
            };
            
            await _redisCacheService.SetData(redisKey, result);

            return View(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> Create([FromRoute] Guid id)
        {
            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(id, UserId);

            if (!groupExistsAndBelongToUser)
                return BadRequest();
            
            ViewData["groupId"] = id;

            return View(new CreateEditViewModel()
            {
                DueDate = DateOnly.FromDateTime(DateTime.Today)
            });
            
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromRoute] Guid id, [FromForm] CreateEditViewModel model)
        {
            try
            {
                bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(id, UserId);

                if (!groupExistsAndBelongToUser)
                    return BadRequest();

                ViewData["groupId"] = id;

                if (!model.Priorities.Contains(model.Priority))
                {
                    ModelState.AddModelError(nameof(model.Priority), InvalidPriorityErrorMessage);
                    return View(model);
                }

                if (!ModelState.IsValid)
                    return View(model);


                CreateEditTodoDto todo = new CreateEditTodoDto()
                {
                    Name = model.Name,
                    Description = model.Description,
                    DueDate = model.DueDate.ToString(DateFormat),
                    Priority = model.Priority,
                    GroupId = id
                };

                await _todoService.AddTodoAsync(todo, UserId);

                List<string> keys = new List<string>();

                foreach (TodosSearchParameter searchParameter in Enum.GetValues<TodosSearchParameter>())
                {
                    if (searchParameter != Completed)
                        keys.AddRange(await _redisCacheService.GetRedisKeysOnPatternAsync(
                            UserId.ToString(),
                            searchParameter.ToString())
                        );
                }
                
                await _redisCacheService.BulkDeleteAsync(keys);
                await _redisCacheService.RemoveData($"dashboard:{UserId}");
                
                return RedirectToAction(nameof(Index), new { id = id });
            }
            catch (DataPersistFail e)
            {
                ModelState.AddModelError(string.Empty, DataPersistFailErrorMessage);
                _logger.LogError(e, string.Format(DataPersistFailErrorMessage, nameof(Create)));

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] Guid id)
        {
            if (id.ToString().IsNullOrEmpty())
                return BadRequest();

            TodoDetailsDto? todo = await _todoService
                .GetTodoDetailsAsync(UserId, id, true);

            if (todo == null)
                return NotFound();

            CreateEditViewModel model = new CreateEditViewModel()
            {
                Name = todo.Name,
                Description = todo.Description,
                DueDate = DateOnly.ParseExact(todo.DueDate, DateFormat, CultureInfo.InvariantCulture),
                Priority = todo.Priority,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromForm] CreateEditViewModel model)
        {
            if (id.ToString().IsNullOrEmpty())
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);
            
            string redisKeyForDetails = $"todoDetails:{UserId}:{id}";

            CreateEditTodoDto todo = new CreateEditTodoDto()
            {
                Name = model.Name,
                Description = model.Description,
                DueDate = model.DueDate.ToString(DateFormat),
                Priority = model.Priority,
            };

            try
            {
                await _todoService.EditTodoAsync(UserId, id, todo);
                
                await _redisCacheService.RemoveData(redisKeyForDetails);
                
                List<string> keys = new List<string>();
                
                foreach (TodosSearchParameter filter in Enum.GetValues<TodosSearchParameter>())
                {
                    keys.AddRange(await _redisCacheService.GetRedisKeysOnPatternAsync(
                        UserId.ToString(),
                        filter.ToString()
                    ));
                }
                
                await _redisCacheService.BulkDeleteAsync(keys);
                await _redisCacheService.RemoveData($"dashboard:{UserId}");
            }
            catch (EntityNotFoundException)
            {
                return BadRequest();
            }
            catch(DataPersistFail e)
            {
                ModelState.AddModelError(string.Empty, DataPersistFailErrorMessage);
                _logger.LogError(e, string.Format(DataPersistFailErrorMessage, nameof(Edit)));

                return View(model);
            }

            return RedirectToAction(nameof(Details), new {id = id});
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromQuery] Guid todoId, [FromQuery] Guid groupId)
        {
            if (todoId.ToString().IsNullOrEmpty())
                return BadRequest();

            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(groupId, UserId);

            if (!groupExistsAndBelongToUser)
                return BadRequest();
            
            string redisKey = $"todoDetails:{UserId}:{todoId}";

            try
            {
                await _todoService.DeleteTodoAsync(UserId, todoId);
                
                await _redisCacheService.RemoveData(redisKey);

                List<string> keys = new List<string>();

                foreach (TodosSearchParameter filter in Enum.GetValues<TodosSearchParameter>())
                {
                    keys.AddRange(await _redisCacheService.GetRedisKeysOnPatternAsync(
                        UserId.ToString(),
                        filter.ToString()));
                }
                
                await _redisCacheService.BulkDeleteAsync(keys);
                await _redisCacheService.RemoveData($"dashboard:{UserId}");
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
            catch(DataPersistFail e)
            {
                _logger.LogError(e, DataPersistFailErrorMessage);

                // add to appear message

                return RedirectToAction(nameof(Index), new { id = groupId });
            }

            return RedirectToAction(nameof(Index), new { id = groupId });
        }

        [HttpPost]
        public async Task<IActionResult> Activate(
            [FromRoute] Guid id,
            [FromQuery] Guid groupId,
            [FromQuery] int page = 0,
            [FromQuery] TodosSearchParameter filter = All)
        {
            if (id.ToString().IsNullOrEmpty())
                return BadRequest();
            
            string redisKey = $"todoDetails:{UserId}:{id}";

            try
            {
                await _todoService.ActivateTodoAsync(UserId, id);

                await _redisCacheService.RemoveData(redisKey);

                List<string> keys = new List<string>();

                foreach (TodosSearchParameter parameter in Enum.GetValues<TodosSearchParameter>())
                {
                    keys.AddRange(await _redisCacheService.GetRedisKeysOnPatternAsync(
                        UserId.ToString(),
                        parameter.ToString()));
                }

                await _redisCacheService.BulkDeleteAsync(keys);
                await _redisCacheService.RemoveData($"dashboard:{UserId}");
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
            catch (DataPersistFail e)
            {
                _logger.LogError(e, DataPersistFailErrorMessage);
                
                return RedirectToAction(nameof(Index), new { id = groupId, page, filter });
            }

            return RedirectToAction(nameof(Index), new { id = groupId, page, filter });
        }

        [HttpPost]
        public async Task<IActionResult> Complete(
            [FromRoute] Guid id,
            [FromQuery] Guid groupId,
            [FromQuery] int page = 0,
            [FromQuery] TodosSearchParameter filter = All)
        {
            if (id.ToString().IsNullOrEmpty())
                return BadRequest();

            string redisKey = $"todoDetails:{UserId}:{id}";
            
            try
            {
                await _todoService.CompleteTodo(id, UserId);

                await _redisCacheService.RemoveData(redisKey);

                List<string> keys = new List<string>();

                foreach (TodosSearchParameter parameter in Enum.GetValues<TodosSearchParameter>())
                {
                    keys.AddRange(await _redisCacheService.GetRedisKeysOnPatternAsync(
                        UserId.ToString(),
                        parameter.ToString()));
                }

                await _redisCacheService.BulkDeleteAsync(keys);
                await _redisCacheService.RemoveData($"dashboard:{UserId}");
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
            catch (DataPersistFail e)
            {
                _logger.LogError(e, DataPersistFailErrorMessage);

                // add to appear message

                return RedirectToAction(nameof(Index), new { id = groupId, page, filter });
            }

            return RedirectToAction(nameof(Index), new { id = groupId, page, filter });
        }

        [HttpGet]
        public async Task<IActionResult> TodosByStatus(
            [FromQuery] TodosSearchParameter filter, [FromQuery] int page = 0)
        {
            if (filter != Pending && filter != Completed)
                return NotFound();
            
            string redisKey = $"All{filter}:todos:{UserId}:{page}";
            
            PagedTodosViewModel? result = await _redisCacheService
                .GetData<PagedTodosViewModel>(redisKey);

            if (result != null)
                return View(result);
            
            PagedTodosDto<TodosAfterDuelDate> allTodos = await _todoService
                .GetTodosFromAllGroups(page, UserId, filter);
            
            int countTodos = await _todoService
                .CountTodos(UserId, filter);
            
            IEnumerable<TodoWithGroupNameViewModel> todos = allTodos.Todos.Select(t => new TodoWithGroupNameViewModel()
            {
                Id = t.Id,
                Name = t.Name,
                DueDate = t.DueDate,
                Priority = t.Priority,
                Status = t.Status,
                GroupName = t.GroupName,
                GroupId = t.GroupId
            });

            result = new PagedTodosViewModel()
            {
                Todos = todos,
                GroupId = allTodos.Todos.FirstOrDefault()?.GroupId ?? Guid.Empty,
                GroupName = allTodos.GroupName,
                PageNumber = page + 1,
                TotalCount = countTodos
            };
            
            if(result.TotalCount > 0)
                await _redisCacheService.SetData(redisKey, result);
            
            return View(result);

        }
    }
}
