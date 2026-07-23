using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories;

public class SupportRepository: BaseRepository, ISupportRepository
{
    public SupportRepository(TodoDbContext dbContext) : base(dbContext)
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
            .ThenBy(s => s.Title)
            .ThenBy(s => s.IsHandled);

        if (projection != null)
        {
            messages = messages.Select(projection);
        }

        return await messages.ToArrayAsync();
    }

    public async Task<bool> CreateSupportMessage(SupportMessage message)
    {
        await DbContext.SupportMessages.AddAsync(message);

        int result = await SaveChangesAsync();

        return result == 1;
    }

    public async Task<bool> EditSupportMessage(SupportMessage message)
    {
        DbContext.SupportMessages.Update(message);

        int result = await SaveChangesAsync();

        return result == 1;
    }

    public async Task<SupportMessage?> GetSupportMessage(
        int id,
        Expression<Func<SupportMessage, SupportMessage>>? projection,
        bool tracked = true,
        bool ignoreQueryFilter = false)
    {
        IQueryable<SupportMessage> message = DbContext
            .SupportMessages
            .Include(u=> u.ApplicationUser)
            .AsQueryable();

        if (!tracked)
        {
            message = message.AsNoTracking();
        }

        if (ignoreQueryFilter)
        {
            message = message.IgnoreQueryFilters();
        }

        if (projection != null)
        {
            message = message.Select(projection);
        }

        return await message.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> DeleteSupportMessage(SupportMessage message)
    {
        DbContext.SupportMessages.Remove(message);

        int result = await SaveChangesAsync();

        return result == 1;
    }
}