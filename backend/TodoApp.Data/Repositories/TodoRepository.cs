using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories;

public class TodoRepository: BaseRepository, ITodoRepository
{
    public TodoRepository(TodoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<TodoEntity>> GetAllTodoNoTracking(
        Expression<Func<TodoEntity, bool>>? filterQuery = null,
        Expression<Func<TodoEntity, TodoEntity>>? projectionQuery = null,
        bool ignoreQueryFilter = false,
        bool includeGroup = false)
    {
        IQueryable<TodoEntity> todos = DbContext
            .Todos
            .AsNoTracking();

        if (includeGroup)
        {
            todos = todos
                .Include(g => g.Group);
        }

        if (ignoreQueryFilter)
        {
            todos = todos
                .IgnoreQueryFilters()
                .OrderBy(t => t.Status)
                .ThenBy(t => t.Priority)
                .ThenBy(t => t.DueDate);
        }
        else
        {
            todos = todos
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.DueDate);
        }

        if (filterQuery != null)
        {
            todos = todos
                .Where(filterQuery);
        }

        if (projectionQuery != null)
        {
            todos = todos
                .Select(projectionQuery)
                .AsQueryable();
        }

        return await todos.ToArrayAsync();
    }

    public async Task<TodoEntity?> GetTodoAsync(Expression<Func<TodoEntity, bool>> filterQuery, bool ignoreQueryFilter = false, bool tracking = false)
    {
        IQueryable<TodoEntity> todoQuery = DbContext
            .Todos
            .AsQueryable();

        if (ignoreQueryFilter)
        {
            todoQuery = todoQuery.IgnoreQueryFilters();
        }

        if (!tracking)
        {
            todoQuery = todoQuery
                .AsNoTracking()
                .Include(g => g.Group)
                .Include(t => t.Comments);
        }

        TodoEntity? todo = await todoQuery
            .SingleOrDefaultAsync(filterQuery);

        return todo;
    }

    public async Task<bool> AddTodoAsync(TodoEntity entity)
    {
        await DbContext.Todos.AddAsync(entity);
        int result = await SaveChangesAsync();

        return result == 1;
    }

    public async Task<bool> EditTodoAsync(TodoEntity entity)
    {
        DbContext.Todos.Update(entity);
        int result = await SaveChangesAsync();

        return result == 1;
    }

    public async Task<bool> DeleteTodoAsync(TodoEntity entity)
    {
        DbContext.Todos.Remove(entity);
        int result = await SaveChangesAsync();

        return result == 1;
    }

    public async Task<int> CountTodosAsync(Expression<Func<TodoEntity, bool>> filter, bool onlyCompleted = false)
    {
        IQueryable<TodoEntity> countTodos = DbContext
            .Todos
            .AsNoTracking()
            .Where(filter);

        if (onlyCompleted)
        {
            return await countTodos
                .IgnoreQueryFilters()
                .CountAsync();
        }

        return await countTodos.CountAsync();
    }
}