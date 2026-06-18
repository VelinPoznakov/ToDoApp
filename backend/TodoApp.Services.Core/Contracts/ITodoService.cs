using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core.Contracts;

public interface ITodoService
{
    Task<IEnumerable<AllTodoDto>> GetAllTodosOrderByPriorityDueDateAsync(Guid userId, Guid groupId, bool ignoreQueryFilter = false, bool onlyCompleted = false);
    Task<TodoDetailsDto?> GetTodoDetailsAsync(Guid userId, Guid todoId, bool track = false);
    Task AddTodoAsync(CreateEditTodoDto createTodoDto, Guid userId);
    Task EditTodoAsync(Guid userId, Guid todoId, CreateEditTodoDto editTodoDto);
    Task ActivateTodoAsync(Guid userId, Guid todoId);
    Task DeleteTodoAsync(Guid userId, Guid todoId);
    Task<int> CountTodos(Guid userId, bool filter = false);
    Task<IEnumerable<TodosAfterDuelDate>> GetTodosAfterDueDate(Guid userId);
    Task CompleteTodo(Guid todoId, Guid userId);
}