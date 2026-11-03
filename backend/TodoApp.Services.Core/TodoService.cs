using System.Linq.Expressions;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

using static TodoApp.GCommon.ApplicationConstants.TodosSearchParameter;
using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Core;

public class TodoService: ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private  readonly IGroupRepository _groupRepository;

    public TodoService(ITodoRepository todoRepository,  IGroupRepository groupRepository)
    {
        _todoRepository = todoRepository;
        _groupRepository = groupRepository;
    }

    public async Task<PagedTodosDto<AllTodoDto>> GetTodosOrderByPriorityDueDateAsync(
        Guid userId,
        Guid groupId,
        int page,
        TodosSearchParameter filter)
    {
        Status? status = filter switch
        {
            All => null,
            Completed => Status.Completed,
            Pending => Status.Pending,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };

        Expression<Func<TodoEntity, bool>> whereParams =
            u => u.UserId == userId
                 && u.Group.Id == groupId
                 && (status == null || u.Status == status);

        IEnumerable<TodoEntity> todos = await _todoRepository
            .GetAllTodoNoTracking(
                page: page,
                filterQuery: whereParams,
                projectionQuery: t => new TodoEntity
                {
                    Id = t.Id,
                    Name = t.Name,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    Group = t.Group
                },
                ignoreQueryFilter: filter != Pending,
                includeGroup: true
            );
        
        IEnumerable<AllTodoDto> result = todos.Select(t => new AllTodoDto()
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            DueDate = t.DueDate.ToString(DateFormat)
        });
        
        PagedTodosDto<AllTodoDto> pagedTodosDto = new PagedTodosDto<AllTodoDto>()
        {
            Todos = result,
            GroupName = todos.FirstOrDefault()?.Group?.Name ?? string.Empty,
            PageNumber = page
        };

        return pagedTodosDto;
    }

    public async Task<TodoDetailsDto?> GetTodoDetailsAsync(
        Guid userId, Guid todoId, bool track = false)
    {
        Expression<Func<TodoEntity, bool>> filter = t => t.UserId == userId && t.Id == todoId;

        TodoEntity? todoEntity = await _todoRepository
            .GetTodoAsync(
                filterQuery: filter,
                ignoreQueryFilter: true,
                tracking: track
            );

        if (todoEntity == null)
        {
            return null;
        }

        TodoDetailsDto result = new TodoDetailsDto()
        {
            Name = todoEntity.Name,
            Description = todoEntity.Description,
            Priority = todoEntity.Priority.ToString(),
            DueDate = todoEntity.DueDate.ToString(DateFormat),
            GroupId = todoEntity.GroupId
        };

        if (!track)
        {
            result.CreatedOn = DateOnly.FromDateTime(todoEntity.CreatedOn).ToString(DateFormat);
            result.GroupName = todoEntity.Group.Name;
            result.Status = todoEntity.Status.ToString();
            result.Comments = todoEntity.Comments.Select(c => new CommentDto()
            {
                Id = c.Id,
                Content = c.Content,
            }).ToList();
        }

        return result;
    }

    public async Task AddTodoAsync(CreateEditTodoDto createTodoDto, Guid userId)
    {
        TodoEntity todo = new TodoEntity()
        {
            Name = createTodoDto.Name,
            Description = createTodoDto.Description,
            Priority = Enum.Parse<Priority>(createTodoDto.Priority),
            DueDate = DateOnly.ParseExact(createTodoDto.DueDate, DateFormat),
            Status = Status.Pending,
            CreatedOn = DateTime.UtcNow,
            UserId = userId,
            GroupId = createTodoDto.GroupId
        };

        bool success = await _todoRepository.AddTodoAsync(todo);

        if (!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task EditTodoAsync(Guid userId, Guid todoId, CreateEditTodoDto editTodoDto)
    {
        Expression<Func<TodoEntity, bool>> filter = t => t.UserId == userId && t.Id == todoId;

        TodoEntity? todoEntity = await _todoRepository
            .GetTodoAsync(
                filterQuery: filter,
                ignoreQueryFilter: false,
                tracking: true
            );

        if (todoEntity == null)
        {
            throw new EntityNotFoundException();
        }

        todoEntity.Name = editTodoDto.Name;
        todoEntity.Description = editTodoDto.Description;
        todoEntity.Priority = Enum.Parse<Priority>(editTodoDto.Priority);
        todoEntity.DueDate = DateOnly.ParseExact(editTodoDto.DueDate, DateFormat);
        todoEntity.UpdatedOn = DateTime.UtcNow;

        bool success = await _todoRepository.EditTodoAsync(todoEntity);

        if (!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task ActivateTodoAsync(Guid userId, Guid todoId)
    {
        TodoEntity? todo = await _todoRepository
            .GetTodoAsync(
                filterQuery: t => t.UserId == userId && t.Id == todoId,
                ignoreQueryFilter: true,
                tracking: true
            );

        if (todo == null)
        {
            throw new EntityNotFoundException();
        }

        todo.Status = Status.Pending;

        bool success = await _todoRepository.EditTodoAsync(todo);

        if(!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task DeleteTodoAsync(Guid userId, Guid todoId)
    {
        TodoEntity? todo = await _todoRepository
            .GetTodoAsync(
                filterQuery: t => t.UserId == userId && t.Id == todoId,
                ignoreQueryFilter: true,
                tracking: true
            );

        if (todo == null)
        {
            throw new EntityNotFoundException();
        }

        bool success = await _todoRepository.DeleteTodoAsync(todo);

        if (!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task<int> CountTodos(Guid userId, TodosSearchParameter filter = All, Guid? groupId = null)
    {
        Status? status = filter switch
        {
            All => null,
            Completed => Status.Completed,
            Pending => Status.Pending,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };

        Expression<Func<TodoEntity, bool>> whereFilter =
            u => u.UserId == userId
                 && (groupId == null || u.Group.Id == groupId)
                 && (status == null || u.Status == status);

        return await _todoRepository.CountTodosAsync(
            filter: whereFilter,
            ignoreQueryFilter: true);
    }

    public async Task<IEnumerable<TodosAfterDuelDate>> GetTodosAfterDueDate(Guid userId)
    {
        Expression<Func<TodoEntity, bool>> filter = 
            t => t.UserId == userId && t.DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

        // TODO: change this later
        IEnumerable<TodoEntity> todos = await _todoRepository
            .GetAllTodoNoTracking(
                filterQuery: filter,
                includeGroup: true,
                projectionQuery: t => new TodoEntity()
                {
                    Id = t.Id,
                    Name = t.Name,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    GroupId = t.GroupId,
                    Group = t.Group
                }
            );

        IEnumerable<TodosAfterDuelDate> result = todos.Select(t => new TodosAfterDuelDate()
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            DueDate = t.DueDate.ToString(DateFormat),
            GroupName = t.Group.Name,
            GroupId = t.GroupId
        });

        return result;
    }

    public async Task CompleteTodo(Guid todoId, Guid userId)
    {
        Expression<Func<TodoEntity, bool>> filter = t => t.Id == todoId && t.UserId == userId;

        TodoEntity? todo = await _todoRepository
            .GetTodoAsync(filterQuery: filter, tracking: true);

        if (todo == null)
        {
            throw new EntityNotFoundException();
        }

        todo.Status = Status.Completed;

        bool edited = await _todoRepository
            .EditTodoAsync(todo);

        if (!edited)
        {
            throw new DataPersistFail();
        }
    }

    public async Task<PagedTodosDto<TodosAfterDuelDate>> GetTodosFromAllGroups(
        int page,
        Guid userId,
        TodosSearchParameter filter)
    {
        Status? status = filter switch
        {
            Completed => Status.Completed,
            Pending => Status.Pending,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
        
        Expression<Func<TodoEntity, bool>> whereParams =
            u => u.UserId == userId
                 && (status == null || u.Status == status);

        IEnumerable<TodoEntity> todos = await _todoRepository
            .GetAllTodoNoTracking(
                page: page,
                filterQuery: whereParams,
                projectionQuery: t => new TodoEntity
                {
                    Id = t.Id,
                    Name = t.Name,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    GroupId = t.GroupId,
                    Group = t.Group
                },
                ignoreQueryFilter: filter != Pending,
                includeGroup: true
            );
        
        IEnumerable<TodosAfterDuelDate> result = todos.Select(t => new TodosAfterDuelDate()
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            DueDate = t.DueDate.ToString(DateFormat),
            GroupName = t.Group.Name,
            GroupId = t.GroupId
        });
        
        PagedTodosDto<TodosAfterDuelDate> pagedTodosDto = new PagedTodosDto<TodosAfterDuelDate>()
        {
            Todos = result,
            GroupName = result.FirstOrDefault()?.GroupName ?? string.Empty,
            PageNumber = page
        };

        return pagedTodosDto;
    }
}