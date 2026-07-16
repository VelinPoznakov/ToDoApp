using System.Linq.Expressions;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

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

    public async Task<(IEnumerable<AllTodoDto>, string groupName)> GetAllTodosOrderByPriorityDueDateAsync(
        Guid userId, Guid groupId)
    {
        Expression<Func<TodoEntity, bool>> filter = u => u.UserId == userId
                                                         && u.Group.Id == groupId;

        IEnumerable<TodoEntity> todos = await _todoRepository
            .GetAllTodoNoTracking(
                filterQuery: filter,
                projectionQuery: t => new TodoEntity
                {
                    Id = t.Id,
                    Name = t.Name,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate
                },
                ignoreQueryFilter: true
            );
        
        Group? group = await _groupRepository.GetGroupById(
            groupId,
            projection: g => new Group()
            {
                Name = g.Name
            },
            false
        );

        if (group == null)
        {
            throw new EntityNotFoundException();
        }
        
        IEnumerable<AllTodoDto> result = todos.Select(t => new AllTodoDto()
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            DueDate = t.DueDate.ToString(DateFormat)
        });

        return (result, group.Name);
    }

    public async Task<(IEnumerable<AllTodoDto>, string groupName)> GetAllCompletedOrderByPriorityDueDateAsync(Guid userId, Guid groupId)
    {
        IEnumerable<TodoEntity> todos = await _todoRepository
            .GetAllTodoNoTracking(
                filterQuery: t => t.UserId == userId
                                  && t.Group.Id == groupId
                                  && t.Status == Status.Completed,
                projectionQuery: t => new TodoEntity
                {
                    Id = t.Id,
                    Name = t.Name,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                },
                ignoreQueryFilter: true
            );

        Group? group = await _groupRepository.GetGroupById(
            groupId,
            projection: g => new Group()
            {
                Name = g.Name
            },
            false
        );

        if (group == null)
        {
            throw new EntityNotFoundException();
        }
        
        IEnumerable<AllTodoDto> result = todos.Select(t => new AllTodoDto()
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            DueDate = t.DueDate.ToString(DateFormat)
        });

        return (result, group.Name);
    }

    public async Task<(IEnumerable<AllTodoDto>, string groupName)> GetAllPendingTodosOrderByPriorityDueDateAsync(Guid userId, Guid groupId)
    {
        IEnumerable<TodoEntity> todos = await _todoRepository
            .GetAllTodoNoTracking(
                filterQuery: t => t.UserId == userId
                                  && t.Group.Id == groupId,
                projectionQuery: t => new TodoEntity
                {
                    Id = t.Id,
                    Name = t.Name,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                }
            );

        Group? group = await _groupRepository.GetGroupById(
            groupId,
            projection: g => new Group()
            {
                Name = g.Name
            },
            false
        );

        if (group == null)
        {
            throw new EntityNotFoundException();
        }
        
        IEnumerable<AllTodoDto> result = todos.Select(t => new AllTodoDto()
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            DueDate = t.DueDate.ToString(DateFormat)
        });

        return (result, group.Name);
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

    public async Task<int> CountTodos(Guid userId, bool onlyCompleted = false)
    {
        Expression<Func<TodoEntity, bool>> whereFilter;
        
        if (onlyCompleted)
        {
            whereFilter = t => t.UserId == userId
                               && t.Status == Status.Completed;

            return await _todoRepository
                .CountTodosAsync(whereFilter,
                    countCompleted: onlyCompleted);
        }

        whereFilter = t => t.UserId == userId;

        return await _todoRepository
            .CountTodosAsync(
                filter: whereFilter,
                countCompleted: onlyCompleted
            );
    }

    public async Task<IEnumerable<TodosAfterDuelDate>> GetTodosAfterDueDate(Guid userId)
    {
        Expression<Func<TodoEntity, bool>> filter = 
            t => t.UserId == userId && t.DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

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
}