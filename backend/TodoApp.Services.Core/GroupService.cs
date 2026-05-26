using TodoApp.Data.Repositories.Contracts;
using TodoApp.Models.Data;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core;

public class GroupService: IGroupService
{
    private readonly IGroupRepository _groupRepository;

    public GroupService(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<IEnumerable<GroupDto>> GetAllGroupsAsync(Guid userId)
    {
        IEnumerable<Group> groups = await _groupRepository
            .GetAllAsync(userId,
                g => new Group()
                {
                    Id = g.Id,
                    Name = g.Name
                });

        IEnumerable<GroupDto> result = groups.Select(g => new GroupDto()
        {
            Id = g.Id,
            Name = g.Name
        });

        return result;
    }

    public async Task<int> GetAllGroupsCount(Guid userId)
    {
        return await _groupRepository
            .GetCountAsync(userId);
    }

    public async Task<bool> GroupExistsAsync(Guid groupId, Guid userId)
    {
        return await _groupRepository
            .ExistsAsync(g => g.Id == groupId && g.UserId == userId);
    }
}