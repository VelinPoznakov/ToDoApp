using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories;

public class GroupRepository: BaseRepository, IGroupRepository
{
    protected GroupRepository(TodoDbContext dbContext) : base(dbContext)
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
}