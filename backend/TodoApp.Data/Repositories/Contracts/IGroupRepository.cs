using System.Linq.Expressions;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories.Contracts;

public interface IGroupRepository
{
    Task<IEnumerable<Group>> GetAllAsync(Guid userId, Expression<Func<Group, Group>>? projection = null);
    Task<int> GetCountAsync(Guid userId);

    Task<bool> ExistsAsync(Expression<Func<Group, bool>> filter);
}