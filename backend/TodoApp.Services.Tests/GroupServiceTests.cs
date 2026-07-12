using System.Linq.Expressions;
using Moq;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Services.Core;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Tests;

// Service-level tests mock IGroupRepository so only the service's own logic is
// under test (mapping, UpdatedOn stamping, failure -> exception translation).
public class GroupServiceTests
{
    private readonly Mock<IGroupRepository> _repo = new();

    private GroupService CreateService() => new GroupService(_repo.Object);

    // ===== GetAllGroupsAsync / counts / exists =====

    [Fact]
    public async Task GetAllGroupsAsync_MapsIdAndName()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        _repo.Setup(r => r.GetAllAsync(userId, It.IsAny<Expression<Func<Group, Group>>?>()))
             .ReturnsAsync(new[] { new Group { Id = groupId, Name = "Work" } });

        var service = CreateService();
        GroupDto[] result = (await service.GetAllGroupsAsync(userId)).ToArray();

        Assert.Single(result);
        Assert.Equal(groupId, result[0].Id);
        Assert.Equal("Work", result[0].Name);
    }

    [Fact]
    public async Task GetAllGroupsCount_PassesThrough()
    {
        Guid userId = Guid.NewGuid();
        _repo.Setup(r => r.GetCountAsync(userId)).ReturnsAsync(7);

        var service = CreateService();

        Assert.Equal(7, await service.GetAllGroupsCount(userId));
    }

    [Fact]
    public async Task GroupExistsAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Group, bool>>>()))
             .ReturnsAsync(true);

        var service = CreateService();

        Assert.True(await service.GroupExistsAsync(Guid.NewGuid(), Guid.NewGuid()));
        _repo.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Group, bool>>>()), Times.Once);
    }

    // ===== CreateGroup =====

    [Fact]
    public async Task CreateGroup_BuildsGroupAndReturnsItsId()
    {
        Guid userId = Guid.NewGuid();
        Group? captured = null;

        _repo.Setup(r => r.CreateGroup(It.IsAny<Group>()))
             .Callback<Group>(g => captured = g)
             .ReturnsAsync(true);

        var service = CreateService();
        Guid newId = await service.CreateGroup(new CreateEditGroupDto { GroupName = "Fresh" }, userId);

        Assert.NotNull(captured);
        Assert.Equal("Fresh", captured!.Name);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal(captured.Id, newId);
        Assert.NotEqual(Guid.Empty, newId);
    }

    [Fact]
    public async Task CreateGroup_WhenRepoFails_ThrowsDataPersistFail()
    {
        _repo.Setup(r => r.CreateGroup(It.IsAny<Group>())).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(
            () => service.CreateGroup(new CreateEditGroupDto { GroupName = "X" }, Guid.NewGuid()));
    }

    // ===== DeleteGroupAsync =====

    [Fact]
    public async Task DeleteGroupAsync_WhenGroupMissing_ThrowsEntityNotFound()
    {
        _repo.Setup(r => r.GetGroupByIdWithTodosAsync(It.IsAny<Guid>()))
             .ReturnsAsync((Group?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.DeleteGroupAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteGroupAsync_GroupWithTodos_UsesDeleteWithTodos()
    {
        Guid id = Guid.NewGuid();
        Group group = new Group { Id = id, Name = "Busy", UserId = Guid.NewGuid() };
        group.Todos.Add(new TodoEntity { Id = Guid.NewGuid(), Name = "T", Description = "d" });

        _repo.Setup(r => r.GetGroupByIdWithTodosAsync(id)).ReturnsAsync(group);
        _repo.Setup(r => r.DeleteGroupWithTodos(group)).ReturnsAsync(true);

        var service = CreateService();
        await service.DeleteGroupAsync(id);

        _repo.Verify(r => r.DeleteGroupWithTodos(group), Times.Once);
        _repo.Verify(r => r.DeleteGroupAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroupAsync_EmptyGroup_UsesPlainDelete()
    {
        Guid id = Guid.NewGuid();
        Group group = new Group { Id = id, Name = "Empty", UserId = Guid.NewGuid() };

        _repo.Setup(r => r.GetGroupByIdWithTodosAsync(id)).ReturnsAsync(group);
        _repo.Setup(r => r.DeleteGroupAsync(group)).ReturnsAsync(true);

        var service = CreateService();
        await service.DeleteGroupAsync(id);

        _repo.Verify(r => r.DeleteGroupAsync(group), Times.Once);
        _repo.Verify(r => r.DeleteGroupWithTodos(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroupAsync_WhenRepoFails_ThrowsDataPersistFail()
    {
        Guid id = Guid.NewGuid();
        Group group = new Group { Id = id, Name = "Empty", UserId = Guid.NewGuid() };

        _repo.Setup(r => r.GetGroupByIdWithTodosAsync(id)).ReturnsAsync(group);
        _repo.Setup(r => r.DeleteGroupAsync(group)).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(() => service.DeleteGroupAsync(id));
    }

    // ===== GetGroupNameForEditTracked =====

    [Fact]
    public async Task GetGroupNameForEditTracked_ReturnsGroupName()
    {
        Guid id = Guid.NewGuid();
        _repo.Setup(r => r.GetGroupById(
                    id,
                    It.IsAny<Expression<Func<Group, Group>>?>(),
                    It.IsAny<bool>()))
             .ReturnsAsync(new Group { Id = id, Name = "Reading" });

        var service = CreateService();
        CreateEditGroupDto dto = await service.GetGroupNameForEditTracked(id);

        Assert.Equal("Reading", dto.GroupName);
    }

    [Fact]
    public async Task GetGroupNameForEditTracked_WhenNotFound_ThrowsEntityNotFound()
    {
        Guid id = Guid.NewGuid();
        _repo.Setup(r => r.GetGroupById(
                    id,
                    It.IsAny<Expression<Func<Group, Group>>?>(),
                    It.IsAny<bool>()))
             .ReturnsAsync((Group?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.GetGroupNameForEditTracked(id));
    }

    // ===== EditGroup =====

    [Fact]
    public async Task EditGroup_UpdatesNameAndStampsUpdatedOn()
    {
        Guid id = Guid.NewGuid();
        Group tracked = new Group { Id = id, Name = "Before", UserId = Guid.NewGuid() };

        _repo.Setup(r => r.GetGroupById(id, null, true))
             .ReturnsAsync(tracked);
        _repo.Setup(r => r.EditGroup(It.IsAny<Group>()))
             .ReturnsAsync(true);

        var service = CreateService();
        await service.EditGroup(id, new CreateEditGroupDto { GroupName = "After" });

        Assert.Equal("After", tracked.Name);
        Assert.NotNull(tracked.UpdatedOn);
        _repo.Verify(r => r.EditGroup(It.Is<Group>(g => g.Name == "After")), Times.Once);
    }

    [Fact]
    public async Task EditGroup_WhenGroupMissing_ThrowsEntityNotFound()
    {
        // A missing group should surface as the domain exception,
        // not as a NullReferenceException.
        Guid id = Guid.NewGuid();
        _repo.Setup(r => r.GetGroupById(id, null, true))
             .ReturnsAsync((Group?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.EditGroup(id, new CreateEditGroupDto { GroupName = "Y" }));
    }

    [Fact]
    public async Task EditGroup_WhenRepoReturnsFalse_ThrowsDataPersistFail()
    {
        Guid id = Guid.NewGuid();
        _repo.Setup(r => r.GetGroupById(id, null, true))
             .ReturnsAsync(new Group { Id = id, Name = "X", UserId = Guid.NewGuid() });
        _repo.Setup(r => r.EditGroup(It.IsAny<Group>()))
             .ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(
            () => service.EditGroup(id, new CreateEditGroupDto { GroupName = "Y" }));
    }
}
