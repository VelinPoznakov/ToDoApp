using TodoApp.GCommon;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core.Contracts;

public interface ITodoService
{
    Task<PagedTodosDto<AllTodoDto>> GetTodosOrderByPriorityDueDateAsync(
        Guid userId,
        Guid groupId,
        int page,
        ApplicationConstants.TodosSearchParameter filter);
    Task<TodoDetailsDto?> GetTodoDetailsAsync(Guid userId, Guid todoId, bool track = false);
    Task AddTodoAsync(CreateEditTodoDto createTodoDto, Guid userId);
    Task EditTodoAsync(Guid userId, Guid todoId, CreateEditTodoDto editTodoDto);
    Task ActivateTodoAsync(Guid userId, Guid todoId);
    Task DeleteTodoAsync(Guid userId, Guid todoId);
    Task<int> CountTodos(
        Guid userId,
        ApplicationConstants.TodosSearchParameter filter = ApplicationConstants.TodosSearchParameter.All,
        Guid? groupId = null);
    Task<IEnumerable<TodosAfterDuelDate>> GetTodosAfterDueDate(Guid userId);
    Task CompleteTodo(Guid todoId, Guid userId);

    Task<PagedTodosDto<TodosAfterDuelDate>> GetTodosFromAllGroups(
        int page,
        Guid userId,
        ApplicationConstants.TodosSearchParameter filter);
}