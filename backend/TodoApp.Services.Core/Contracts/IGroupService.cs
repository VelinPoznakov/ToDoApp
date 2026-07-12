using System.Linq.Expressions;
using TodoApp.Models.Data;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core.Contracts;

public interface IGroupService
{
    Task<IEnumerable<GroupDto>> GetAllGroupsAsync(Guid userId);

    Task<int> GetAllGroupsCount(Guid userId);

    Task<bool> GroupExistsAsync(Guid groupId, Guid userId);

    Task DeleteGroupAsync(Guid groupId);

    Task<Guid> CreateGroup(CreateEditGroupDto model, Guid userId);

    Task<CreateEditGroupDto> GetGroupNameForEditTracked(Guid id);

    Task EditGroup(Guid id, CreateEditGroupDto model);
}