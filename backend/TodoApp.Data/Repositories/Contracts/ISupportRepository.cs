using System.Linq.Expressions;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories.Contracts;

public interface ISupportRepository
{
    Task<IEnumerable<SupportMessage>> GetAllSupportMessagesNoTracking(
            Expression<Func<SupportMessage, bool>>? filter,
            Expression<Func<SupportMessage, SupportMessage>> projection,
            bool ignoreQueryFilter = false,
            bool includeUser = false);
}