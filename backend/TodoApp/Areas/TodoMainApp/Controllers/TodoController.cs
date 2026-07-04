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

// reformat gcommon dll
// finish the controller
// start the views
// make forgot password
// understand cookies and sessions
// start admin area dev
// try to make about page
// try to make feedback page

namespace TodoApp.Areas.TodoMainApp.Controllers
{
    public class TodoController : BaseController
    {
        private readonly ITodoService _todoService;
        private readonly IGroupService _groupService;
        private readonly ILogger<TodoController> _logger;

        public TodoController(
            ITodoService todoService,
            IGroupService groupService,
            ILogger<TodoController> logger)
        {
            _todoService = todoService;
            _groupService = groupService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute]Guid id)
        {
            if(id.ToString().IsNullOrEmpty())
            {
                return BadRequest();
            }

            Guid userId = Guid.Parse(GetUserId()!);

            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(id, userId);

            if (!groupExistsAndBelongToUser)
            {
                return BadRequest();
            }

            TempData["groupId"] = id;

            IEnumerable<AllTodoDto> alltodos = await _todoService
                .GetAllTodosOrderByPriorityDueDateAsync(userId, id);

            IEnumerable<TodoViewModel> result
                = alltodos.Select(t => new TodoViewModel()
                {
                    Id = t.Id,
                    Name = t.Name,
                    DueDate = t.DueDate,
                    Priority = t.Priority,
                    Status = t.Status
                });

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] Guid id)
        {
            if (id.ToString().IsNullOrEmpty())
            {
                return BadRequest();
            }

            Guid userId = Guid.Parse(GetUserId()!);

            TodoDetailsDto? todo = await _todoService
                .GetTodoDetailsAsync(userId, id);

            if (todo == null)
            {
                return NotFound();
            }

            TodoDetailsViewmodel result = new TodoDetailsViewmodel()
            {
                Name = todo.Name,
                Description = todo.Description,
                Priority = todo.Priority,
                Status = todo.Status,
                DueDate = todo.DueDate,
                GroupName = todo.GroupName,
                CreatedOn = todo.CreatedOn,
                Comments = todo.Comments.Select(c => new CommentViewModel()
                {
                    Id = c.Id,
                    Content = c.Content,
                }).ToList()
            };

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create([FromRoute] Guid groupId)
        {
            Guid userId = Guid.Parse(GetUserId()!);

            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(groupId, userId);

            if (!groupExistsAndBelongToUser)
            {
                return BadRequest();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] Guid groupId, [FromQuery] CreateEditViewModel model)
        {
            Guid userId = Guid.Parse(GetUserId()!);

            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(groupId, userId);

            if (!groupExistsAndBelongToUser)
            {
                return BadRequest();
            }

            if (!model.Priorities.Contains(model.Priority))
            {
                ModelState.AddModelError(nameof(model.Priority), InvalidPriorityErrorMessage);
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                CreateEditTodoDto todo = new CreateEditTodoDto()
                {
                    Name = model.Name,
                    Description = model.Description,
                    DueDate = model.DueDate.ToString(DateFormat),
                    Priority = model.Priority,
                    GroupId = groupId
                };

                await _todoService.AddTodoAsync(todo, userId);
            }
            catch (DataPersistFail e)
            {
                ModelState.AddModelError(string.Empty, DataPersistFailErrorMessage);
                _logger.LogError(e, string.Format(DataPersistFailErrorMessage, nameof(Create)));

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, UnexpectedErrorMessage);
                ModelState.AddModelError(string.Empty, UnexpectedErrorMessage);

                return View(model);
            }

            return RedirectToAction(nameof(Index), new{groupId = groupId});
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] Guid id)
        {
            if (id.ToString().IsNullOrEmpty())
            {
                return BadRequest();
            }

            Guid userId = Guid.Parse(GetUserId()!);

            TodoDetailsDto? todo = await _todoService
                .GetTodoDetailsAsync(userId, id, true);

            if (todo == null)
            {
                return NotFound();
            }

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
            {
                return BadRequest();
            }
            
            Guid userId = Guid.Parse(GetUserId()!);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            CreateEditTodoDto todo = new CreateEditTodoDto()
            {
                Name = model.Name,
                Description = model.Description,
                DueDate = model.DueDate.ToString(DateFormat),
                Priority = model.Priority,
            };

            try
            {
                await _todoService.EditTodoAsync(userId, id, todo);
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
            catch (Exception e)
            {
                _logger.LogError(e, UnexpectedErrorMessage);
                ModelState.AddModelError(string.Empty, UnexpectedErrorMessage);

                return View(model);
            }

            return RedirectToAction(nameof(Details), new {id = id});
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromQuery] Guid todoId)
        {
            if (todoId.ToString().IsNullOrEmpty())
            {
                return BadRequest();
            }

            if (TempData["groupId"] == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            Guid groupId = (Guid)TempData["GroupId"]!;

            Guid userId = Guid.Parse(GetUserId()!);

            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(groupId, userId);

            if (!groupExistsAndBelongToUser)
            {
                return BadRequest();
            }

            try
            {
                await _todoService.DeleteTodoAsync(userId, todoId);
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

        [HttpGet]
        public async Task<IActionResult> Completed([FromRoute] Guid id)
        {
            if (id.ToString().IsNullOrEmpty())
            {
                return BadRequest();
            }

            Guid userId = Guid.Parse(GetUserId()!);

            bool groupExistsAndBelongToUser = await _groupService.GroupExistsAsync(id, userId);

            if (!groupExistsAndBelongToUser)
            {
                return BadRequest();
            }

            IEnumerable<AllTodoDto> allTodos = await _todoService
                .GetAllTodosOrderByPriorityDueDateAsync(
                    userId,
                    id,
                    true,
                    true);

            IEnumerable<TodoViewModel> result = allTodos.Select(t => new TodoViewModel()
            {
                Id = t.Id,
                Name = t.Name,
                DueDate = t.DueDate,
                Priority = t.Priority,
                Status = t.Status
            });

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Activate([FromRoute] Guid id)
        {
            if (id.ToString().IsNullOrEmpty())
            {
                return BadRequest();
            }

            if (TempData["groupId"] == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            Guid userId = Guid.Parse(GetUserId()!);

            Guid groupId = (Guid)TempData.Peek("groupId")!;

            try
            {
                await _todoService.ActivateTodoAsync(userId, id);
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
            catch (DataPersistFail e)
            {
                _logger.LogError(e, DataPersistFailErrorMessage);
                return RedirectToAction(nameof(Index), new { id = groupId });
            }

            return RedirectToAction(nameof(Index), new { id = groupId });
        }

        [HttpPost]
        public async Task<IActionResult> Complete([FromRoute] Guid id)
        {
            if (id.ToString().IsNullOrEmpty())
            {
                return BadRequest();
            }

            if (TempData["groupId"] == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            Guid userId = Guid.Parse(GetUserId()!);
            Guid groupId = (Guid)TempData.Peek("groupId")!;

            try
            {
                await _todoService.CompleteTodo(id, userId);
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
            catch (DataPersistFail e)
            {
                _logger.LogError(e, DataPersistFailErrorMessage);

                // add to appear message

                return RedirectToAction(nameof(Completed), new { id = groupId });
            }

            return RedirectToAction(nameof(Completed), new { id = groupId });
        }
    }
}
