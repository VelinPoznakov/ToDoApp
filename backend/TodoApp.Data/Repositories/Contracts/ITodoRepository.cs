using System.Linq.Expressions;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories.Contracts;

public interface ITodoRepository
{
    Task<IEnumerable<TodoEntity>> GetAllTodoNoTracking(
        int page,
        Expression<Func<TodoEntity, bool>>? filterQuery = null,
        Expression<Func<TodoEntity, TodoEntity>>? projectionQuery = null,
        bool ignoreQueryFilter = false,
        bool includeGroup = false);

    Task<TodoEntity?> GetTodoAsync(Expression<Func<TodoEntity, bool>> filterQuery, bool ignoreQueryFilter = false,
        bool tracking = false);

    Task<bool> AddTodoAsync(TodoEntity entity);

    Task<bool> EditTodoAsync(TodoEntity entity);

    Task<bool> DeleteTodoAsync(TodoEntity entity);

    Task<int> CountTodosAsync(
        Expression<Func<TodoEntity, bool>> filter,
        bool countCompleted = false);
}