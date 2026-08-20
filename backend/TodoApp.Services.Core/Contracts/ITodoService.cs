using TodoApp.Models.Data;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core.Contracts;

public interface ITodoService
{
    Task<PagedTodosDto> GetAllTodosOrderByPriorityDueDateAsync(
        Guid userId,
        Guid groupId,
        int page);
    Task<PagedTodosDto> GetAllCompletedOrderByPriorityDueDateAsync(
        Guid userId,
        Guid groupId,
        int page);
    Task<PagedTodosDto> GetAllPendingTodosOrderByPriorityDueDateAsync(
        Guid userId,
        Guid groupId,
        int page);
    Task<TodoDetailsDto?> GetTodoDetailsAsync(Guid userId, Guid todoId, bool track = false);
    Task AddTodoAsync(CreateEditTodoDto createTodoDto, Guid userId);
    Task EditTodoAsync(Guid userId, Guid todoId, CreateEditTodoDto editTodoDto);
    Task ActivateTodoAsync(Guid userId, Guid todoId);
    Task DeleteTodoAsync(Guid userId, Guid todoId);
    Task<int> CountTodos(Guid userId, bool onlyCompleted = false);
    Task<IEnumerable<TodosAfterDuelDate>> GetTodosAfterDueDate(Guid userId);
    Task CompleteTodo(Guid todoId, Guid userId);
}