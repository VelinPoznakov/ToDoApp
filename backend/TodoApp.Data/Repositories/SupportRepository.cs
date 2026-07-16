using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories;

public class SupportRepository: BaseRepository, ISupportRepository
{
    protected SupportRepository(TodoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<SupportMessage>> GetAllSupportMessagesNoTracking(
        Expression<Func<SupportMessage, bool>>? filter,
        Expression<Func<SupportMessage, SupportMessage>>? projection,
        bool ignoreQueryFilter = false,
        bool includeUser = false)
    {
        IQueryable<SupportMessage> messages = DbContext
            .SupportMessages
            .AsNoTracking();

        if (ignoreQueryFilter)
        {
            messages = messages.IgnoreQueryFilters();
        }

        if (includeUser)
        {
            messages = messages.Include(u => u.ApplicationUser);
        }

        if (filter != null)
        {
            messages = messages.Where(filter);
        }

        messages = messages
            .OrderBy(s => s.CreatedOn)
            .ThenBy(s => s.Title);

        if (projection != null)
        {
            messages = messages.Select(projection);
        }

        return await messages.ToArrayAsync();
    }
}