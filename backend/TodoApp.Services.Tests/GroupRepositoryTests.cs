using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Data.Repositories;
using TodoApp.Models.Data;
using TodoApp.Models.Data.Enums;

namespace TodoApp.Services.Tests;

// Repository-level tests run against EF Core InMemory so the real query logic
// (projection, Include + global query filter, persistence) is exercised.
public class GroupRepositoryTests
{
    // ===== GetAllAsync =====

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyUsersGroups_OrderedByName()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();
        Guid strangerId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(Guid.NewGuid(), userId, "Zebra"));
            seed.Groups.Add(TestDb.NewGroup(Guid.NewGuid(), userId, "Alpha"));
            seed.Groups.Add(TestDb.NewGroup(Guid.NewGuid(), strangerId, "NotMine"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Group[] result = (await repo.GetAllAsync(userId)).ToArray();

        Assert.Equal(2, result.Length);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
        Assert.DoesNotContain(result, g => g.Name == "NotMine");
    }

    [Fact]
    public async Task GetAllAsync_WithProjection_ReturnsProjectedGroups()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(Guid.NewGuid(), userId, "Work"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Group[] result = (await repo.GetAllAsync(
            userId,
            g => new Group { Id = g.Id, Name = g.Name })).ToArray();

        Assert.Single(result);
        Assert.Equal("Work", result[0].Name);
        Assert.NotEqual(Guid.Empty, result[0].Id);
    }

    // ===== GetCountAsync / ExistsAsync =====

    [Fact]
    public async Task GetCountAsync_CountsOnlyUsersGroups()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(Guid.NewGuid(), userId, "One"));
            seed.Groups.Add(TestDb.NewGroup(Guid.NewGuid(), userId, "Two"));
            seed.Groups.Add(TestDb.NewGroup(Guid.NewGuid(), Guid.NewGuid(), "Foreign"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Assert.Equal(2, await repo.GetCountAsync(userId));
    }

    [Fact]
    public async Task ExistsAsync_MatchesOnlyOwnedGroup()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "Mine"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Assert.True(await repo.ExistsAsync(g => g.Id == groupId && g.UserId == userId));
        Assert.False(await repo.ExistsAsync(g => g.Id == groupId && g.UserId == Guid.NewGuid()));
    }

    // ===== GetGroupById =====

    [Fact]
    public async Task GetGroupById_NoProjection_ReturnsTheGroup()
    {
        var options = TestDb.Options();
        Guid id = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(id, Guid.NewGuid(), "Work"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Group? result = await repo.GetGroupById(id);

        Assert.NotNull(result);
        Assert.Equal("Work", result!.Name);
    }

    [Fact]
    public async Task GetGroupById_WithProjection_ReturnsGroup()
    {
        // The service uses this overload for the Edit screen:
        // GetGroupById(id, projection: g => new Group { Name = g.Name })
        var options = TestDb.Options();
        Guid id = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(id, Guid.NewGuid(), "Fitness"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Expression<Func<Group, Group>> projection = g => new Group { Name = g.Name };
        Group? result = await repo.GetGroupById(id, projection);

        Assert.NotNull(result);
        Assert.Equal("Fitness", result!.Name);
    }

    [Fact]
    public async Task GetGroupById_NotTracked_DoesNotTrackTheEntity()
    {
        var options = TestDb.Options();
        Guid id = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(id, Guid.NewGuid(), "Untracked"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Group? result = await repo.GetGroupById(id, tracked: false);

        Assert.NotNull(result);
        Assert.Empty(ctx.ChangeTracker.Entries<Group>());
    }

    // ===== Create / Edit / Delete =====

    [Fact]
    public async Task CreateGroup_PersistsTheGroup()
    {
        var options = TestDb.Options();
        Guid id = Guid.NewGuid();

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new GroupRepository(ctx);
            bool result = await repo.CreateGroup(TestDb.NewGroup(id, Guid.NewGuid(), "Fresh"));
            Assert.True(result);
        }

        await using var verify = new TodoDbContext(options);
        Assert.NotNull(await verify.Groups.FirstOrDefaultAsync(g => g.Id == id));
    }

    [Fact]
    public async Task EditGroup_PersistsNameChange()
    {
        var options = TestDb.Options();
        Guid id = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(id, Guid.NewGuid(), "Old Name"));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new GroupRepository(ctx);
            Group group = (await repo.GetGroupById(id))!;
            group.Name = "New Name";
            group.UpdatedOn = DateTime.Now;

            bool result = await repo.EditGroup(group);
            Assert.True(result);
        }

        await using (var verify = new TodoDbContext(options))
        {
            Group? reloaded = await verify.Groups.FirstOrDefaultAsync(g => g.Id == id);
            Assert.NotNull(reloaded);
            Assert.Equal("New Name", reloaded!.Name);
            Assert.NotNull(reloaded.UpdatedOn);
        }
    }

    [Fact]
    public async Task DeleteGroupAsync_RemovesEmptyGroup()
    {
        var options = TestDb.Options();
        Guid id = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(id, Guid.NewGuid(), "Empty"));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new GroupRepository(ctx);
            Group group = (await repo.GetGroupById(id))!;

            Assert.True(await repo.DeleteGroupAsync(group));
        }

        await using var verify = new TodoDbContext(options);
        Assert.Null(await verify.Groups.FirstOrDefaultAsync(g => g.Id == id));
    }

    [Fact]
    public async Task DeleteGroupWithTodos_RemovesGroupAndItsTodos()
    {
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "Busy"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "A"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "B"));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new GroupRepository(ctx);
            Group group = (await repo.GetGroupByIdWithTodosAsync(groupId))!;

            Assert.True(await repo.DeleteGroupWithTodos(group));
        }

        await using var verify = new TodoDbContext(options);
        Assert.Null(await verify.Groups.FirstOrDefaultAsync(g => g.Id == groupId));
        Assert.Empty(await verify.Todos.IgnoreQueryFilters().Where(t => t.GroupId == groupId).ToArrayAsync());
    }

    [Fact]
    public async Task GetGroupByIdWithTodosAsync_LoadsAllTodos_IncludingCompleted()
    {
        // For a correct group delete, EVERY child todo must be loaded so it can be
        // removed before the group (Todo->Group FK is NoAction). The global query
        // filter (Status == Pending) hides completed todos unless ignored.
        var options = TestDb.Options();
        Guid groupId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Groups.Add(TestDb.NewGroup(groupId, userId, "Mixed"));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Pending one", Status.Pending));
            seed.Todos.Add(TestDb.NewTodo(groupId, userId, "Completed one", Status.Completed));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new GroupRepository(ctx);

        Group? group = await repo.GetGroupByIdWithTodosAsync(groupId);

        Assert.NotNull(group);
        Assert.Equal(2, group!.Todos.Count);
    }
}
