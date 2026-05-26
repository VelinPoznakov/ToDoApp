using System.Linq.Expressions;
using TodoApp.Models.Data;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core.Contracts;

public interface IGroupService
{
    Task<IEnumerable<GroupDto>> GetAllGroupsAsync(Guid userId);

    Task<int> GetAllGroupsCount(Guid userId);

    Task<bool> GroupExistsAsync(Guid groupId, Guid userId);
}