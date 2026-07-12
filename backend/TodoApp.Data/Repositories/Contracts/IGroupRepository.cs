using System.Linq.Expressions;
using TodoApp.Models.Data;

namespace TodoApp.Data.Repositories.Contracts;

public interface IGroupRepository
{
    Task<IEnumerable<Group>> GetAllAsync(Guid userId, Expression<Func<Group, Group>>? projection = null);

    Task<int> GetCountAsync(Guid userId);

    Task<bool> ExistsAsync(Expression<Func<Group, bool>> filter);

    Task<Group?> GetGroupByIdWithTodosAsync(Guid id);

    Task<bool> DeleteGroupAsync(Group group);

    Task<bool> DeleteGroupWithTodos(Group group);

    Task<bool> CreateGroup(Group group);

    Task<Group?> GetGroupById(Guid id, Expression<Func<Group, Group>>? projection = null, bool tracked = true);

    Task<bool> EditGroup(Group group);
}