using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Data.Repositories;
using TodoApp.Models.Data;

namespace TodoApp.Services.Tests;

// Repository tests run against EF Core InMemory so the real query behaviour
// (global query filter, ordering, projection, persistence) is exercised.
public class SupportRepositoryTests
{
    private static ApplicationUser NewUser(Guid id, string first = "John", string last = "Doe")
        => new ApplicationUser
        {
            Id = id,
            UserName = $"{first}{last}@example.com",
            Email = $"{first}{last}@example.com",
            FirstName = first,
            LastName = last
        };

    private static SupportMessage NewMessage(
        Guid userId,
        string title,
        bool isHandled = false,
        DateTime? createdOn = null)
        => new SupportMessage
        {
            Title = title,
            Description = "Description of " + title,
            CreatedOn = createdOn ?? new DateTime(2026, 7, 20, 10, 0, 0),
            IsHandled = isHandled,
            HandledOn = isHandled ? new DateTime(2026, 7, 21, 9, 0, 0) : null,
            ApplicationUserId = userId
        };

    // ===== Create =====

    [Fact]
    public async Task CreateSupportMessage_PersistsTheMessage()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new SupportRepository(ctx);
            Assert.True(await repo.CreateSupportMessage(NewMessage(userId, "Broken button")));
        }

        await using var verify = new TodoDbContext(options);
        SupportMessage? saved = await verify.SupportMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("Broken button", saved!.Title);
        Assert.False(saved.IsHandled);
    }

    // ===== Query filter behaviour =====

    [Fact]
    public async Task GetAll_Default_HidesHandledMessages()
    {
        // The global query filter is IsHandled == false.
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            seed.SupportMessages.Add(NewMessage(userId, "Open one"));
            seed.SupportMessages.Add(NewMessage(userId, "Closed one", isHandled: true));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        SupportMessage[] result = (await repo.GetAllSupportMessagesNoTracking(
            filter: null,
            projection: null!)).ToArray();

        SupportMessage single = Assert.Single(result);
        Assert.Equal("Open one", single.Title);
    }

    [Fact]
    public async Task GetAll_IgnoreQueryFilter_ReturnsHandledToo()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            seed.SupportMessages.Add(NewMessage(userId, "Open one"));
            seed.SupportMessages.Add(NewMessage(userId, "Closed one", isHandled: true));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        SupportMessage[] result = (await repo.GetAllSupportMessagesNoTracking(
            filter: null,
            projection: null!,
            ignoreQueryFilter: true)).ToArray();

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public async Task GetAll_OrdersByCreatedOnAscending()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            seed.SupportMessages.Add(NewMessage(userId, "Newest", createdOn: new DateTime(2026, 7, 25, 8, 0, 0)));
            seed.SupportMessages.Add(NewMessage(userId, "Oldest", createdOn: new DateTime(2026, 7, 1, 8, 0, 0)));
            seed.SupportMessages.Add(NewMessage(userId, "Middle", createdOn: new DateTime(2026, 7, 10, 8, 0, 0)));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        string[] titles = (await repo.GetAllSupportMessagesNoTracking(null, null!))
            .Select(m => m.Title)
            .ToArray();

        Assert.Equal(new[] { "Oldest", "Middle", "Newest" }, titles);
    }

    [Fact]
    public async Task GetAll_FilterAndIncludeUser_ReturnsOnlyThatUsersMessagesWithUserLoaded()
    {
        var options = TestDb.Options();
        Guid mine = Guid.NewGuid();
        Guid other = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(mine, "Ann", "Smith"));
            seed.Users.Add(NewUser(other, "Bob", "Jones"));
            seed.SupportMessages.Add(NewMessage(mine, "Mine"));
            seed.SupportMessages.Add(NewMessage(other, "Theirs"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        SupportMessage[] result = (await repo.GetAllSupportMessagesNoTracking(
            filter: m => m.ApplicationUserId == mine,
            projection: null!,
            includeUser: true)).ToArray();

        SupportMessage single = Assert.Single(result);
        Assert.Equal("Mine", single.Title);
        Assert.NotNull(single.ApplicationUser);
        Assert.Equal("Ann", single.ApplicationUser.FirstName);
    }

    [Fact]
    public async Task GetAll_WithProjection_ReturnsProjectedShape()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            seed.SupportMessages.Add(NewMessage(userId, "Projected"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        SupportMessage[] result = (await repo.GetAllSupportMessagesNoTracking(
            filter: null,
            projection: m => new SupportMessage { Id = m.Id, Title = m.Title })).ToArray();

        SupportMessage single = Assert.Single(result);
        Assert.Equal("Projected", single.Title);
        Assert.NotEqual(0, single.Id);
        // Not selected by the projection.
        Assert.Null(single.Description);
    }

    // ===== GetSupportMessage =====

    [Fact]
    public async Task GetSupportMessage_ReturnsTheMessageById()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();
        int id;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            SupportMessage message = NewMessage(userId, "Find me");
            seed.SupportMessages.Add(message);
            await seed.SaveChangesAsync();
            id = message.Id;
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        SupportMessage? result = await repo.GetSupportMessage(id, projection: null);

        Assert.NotNull(result);
        Assert.Equal("Find me", result!.Title);
    }

    [Fact]
    public async Task GetSupportMessage_HandledOne_IsHiddenUnlessFilterIgnored()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();
        int id;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            SupportMessage message = NewMessage(userId, "Closed", isHandled: true);
            seed.SupportMessages.Add(message);
            await seed.SaveChangesAsync();
            id = message.Id;
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        Assert.Null(await repo.GetSupportMessage(id, projection: null));
        Assert.NotNull(await repo.GetSupportMessage(id, projection: null, ignoreQueryFilter: true));
    }

    [Fact]
    public async Task GetSupportMessage_NotTracked_DoesNotTrackTheEntity()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();
        int id;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            SupportMessage message = NewMessage(userId, "Untracked");
            seed.SupportMessages.Add(message);
            await seed.SaveChangesAsync();
            id = message.Id;
        }

        await using var ctx = new TodoDbContext(options);
        var repo = new SupportRepository(ctx);

        SupportMessage? result = await repo.GetSupportMessage(id, projection: null, tracked: false);

        Assert.NotNull(result);
        Assert.Empty(ctx.ChangeTracker.Entries<SupportMessage>());
    }

    // ===== Edit / Delete =====

    [Fact]
    public async Task EditSupportMessage_PersistsHandledState()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();
        int id;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            SupportMessage message = NewMessage(userId, "To handle");
            seed.SupportMessages.Add(message);
            await seed.SaveChangesAsync();
            id = message.Id;
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new SupportRepository(ctx);
            SupportMessage message = (await repo.GetSupportMessage(id, projection: null))!;
            message.IsHandled = true;
            message.HandledOn = new DateTime(2026, 7, 22, 12, 0, 0);

            Assert.True(await repo.EditSupportMessage(message));
        }

        await using var verify = new TodoDbContext(options);
        SupportMessage? saved = await verify.SupportMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == id);

        Assert.NotNull(saved);
        Assert.True(saved!.IsHandled);
        Assert.Equal(new DateTime(2026, 7, 22, 12, 0, 0), saved.HandledOn);
    }

    [Fact]
    public async Task DeleteSupportMessage_RemovesIt()
    {
        var options = TestDb.Options();
        Guid userId = Guid.NewGuid();
        int id;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewUser(userId));
            SupportMessage message = NewMessage(userId, "Doomed");
            seed.SupportMessages.Add(message);
            await seed.SaveChangesAsync();
            id = message.Id;
        }

        await using (var ctx = new TodoDbContext(options))
        {
            var repo = new SupportRepository(ctx);
            SupportMessage message = (await repo.GetSupportMessage(id, projection: null))!;

            Assert.True(await repo.DeleteSupportMessage(message));
        }

        await using var verify = new TodoDbContext(options);
        Assert.Empty(await verify.SupportMessages.IgnoreQueryFilters().ToArrayAsync());
    }
}
