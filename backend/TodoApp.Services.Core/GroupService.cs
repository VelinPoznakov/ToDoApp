using System.Linq.Expressions;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
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

    public async Task DeleteGroupAsync(Guid groupId)
    {
        Group? group = await _groupRepository
            .GetGroupByIdWithTodosAsync(groupId);

        if (group == null)
        {
            throw new EntityNotFoundException();
        }

        bool result = false;

        if (group.Todos.Count != 0)
        {
            result = await _groupRepository.DeleteGroupWithTodos(group);
        }
        else
        {
            result = await _groupRepository.DeleteGroupAsync(group);
        }

        if (!result)
        {
            throw new DataPersistFail();
        }
    }

    public async Task<Guid> CreateGroup(CreateEditGroupDto model, Guid userId)
    {
        Group newGroup = new Group()
        {
            Id = Guid.NewGuid(),
            Name = model.GroupName,
            UserId = userId,
            CreatedOn = DateTime.Now
        };

        bool result = await _groupRepository.CreateGroup(newGroup);

        if (!result)
        {
            throw new DataPersistFail();
        }

        return newGroup.Id;
    }

    public async Task<CreateEditGroupDto> GetGroupNameForEditTracked(Guid id)
    {
        Group? group = await _groupRepository
            .GetGroupById(id,
                projection: g => new Group()
                {
                    Name = g.Name
                });

        if (group == null)
        {
            throw new EntityNotFoundException();
        }

        CreateEditGroupDto result = new CreateEditGroupDto()
        {
            GroupName = group.Name
        };

        return result;
    }

    public async Task EditGroup(Guid id, CreateEditGroupDto model)
    {
        Group? group = await _groupRepository.GetGroupById(id);

        group!.Name = model.GroupName;
        group!.UpdatedOn = DateTime.Now;

        bool result = await _groupRepository.EditGroup(group);

        if (!result)
        {
            throw new DataPersistFail();
        }
    }
}