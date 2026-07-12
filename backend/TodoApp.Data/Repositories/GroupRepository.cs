using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories;

public class GroupRepository: BaseRepository, IGroupRepository
{
    public GroupRepository(TodoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Group>> GetAllAsync(Guid userId, Expression<Func<Group, Group>>? projection = null)
    {
        IQueryable<Group> groups = DbContext
            .Groups
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Name)
            .AsNoTracking();

        if (projection != null)
        {
            groups = groups
                .Select(projection)
                .AsQueryable();
        }

        return await groups.ToArrayAsync();

    }

    public async Task<int> GetCountAsync(Guid userId)
    {
        return await DbContext
            .Groups
            .Where(g => g.UserId == userId)
            .CountAsync();
    }

    public Task<bool> ExistsAsync(Expression<Func<Group, bool>> filter)
    {
        return DbContext
            .Groups
            .AnyAsync(filter);
    }

    public async Task<Group?> GetGroupByIdWithTodosAsync(Guid id)
    {
        return await DbContext.Groups
            .Include(t => t.Todos)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<bool> DeleteGroupAsync(Group group)
    {
        DbContext.Groups.Remove(group);

        int result = await SaveChangesAsync();

        return result == 1;
    }

    public async Task<bool> DeleteGroupWithTodos(Group group)
    {
        DbContext.Todos.RemoveRange(group.Todos);
        DbContext.Groups.Remove(group);

        int result = await SaveChangesAsync();

        return result == (1 + group.Todos.Count);
    }

    public async Task<bool> CreateGroup(Group group)
    {
        await DbContext.Groups.AddAsync(group);

        int result = await SaveChangesAsync();

        return result == 1;
    }

    public async Task<Group?> GetGroupById(Guid id, Expression<Func<Group, Group>>? projection = null, bool tracked = true)
    {
        IQueryable<Group> group = DbContext.Groups;

        if (!tracked)
        {
            group = group.AsNoTracking();
        }

        if (projection != null)
        {
            return await group
                .Where(g => g.Id == id)
                .Select(projection)
                .FirstOrDefaultAsync();
        }

        return await group
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<bool> EditGroup(Group group)
    {
        DbContext.Groups.Update(group);

        int result = await SaveChangesAsync();

        return result == 1;
    }
}