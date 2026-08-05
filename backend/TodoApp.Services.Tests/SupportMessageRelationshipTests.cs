using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TodoApp.Data;
using TodoApp.Models.Data;

namespace TodoApp.Services.Tests;

// These tests inspect the EF model itself (and the runtime consequences of it)
// rather than service behaviour.
//
// WHY THEY EXIST
// The "handle a support message" flow writes SupportMessage.HandledByAdminUserId.
// Whether that write can succeed depends entirely on which entity the foreign
// key points at - a fact no other test in this suite touches:
//   * SupportServiceTests mocks ISupportRepository, so it never reaches EF.
//   * SupportRepositoryTests uses EF InMemory, which does NOT enforce foreign
//     keys, so a bad key silently "saves".
//
// The tests below therefore assert the shape of the model directly, plus the
// one runtime symptom InMemory *can* show (an unresolvable navigation).
public class SupportMessageRelationshipTests
{
    private static IForeignKey HandlerForeignKey(TodoDbContext ctx)
        => ctx.Model
            .FindEntityType(typeof(SupportMessage))!
            .GetForeignKeys()
            .Single(fk => fk.Properties
                .Any(p => p.Name == nameof(SupportMessage.HandledByAdminUserId)));

    private static IForeignKey AuthorForeignKey(TodoDbContext ctx)
        => ctx.Model
            .FindEntityType(typeof(SupportMessage))!
            .GetForeignKeys()
            .Single(fk => fk.Properties
                .Any(p => p.Name == nameof(SupportMessage.ApplicationUserId)));

    private static ApplicationUser NewPlainUser(Guid id)
        => new ApplicationUser
        {
            Id = id,
            UserName = "admin@todoapp.local",
            Email = "admin@todoapp.local",
            FirstName = "Velin",
            LastName = "Poznakov"
        };

    // ===== The two relationships =====

    [Fact]
    public void SupportMessage_HasSeparateAuthorAndHandlerRelationships()
    {
        using var ctx = new TodoDbContext(TestDb.Options());

        Assert.NotNull(AuthorForeignKey(ctx));
        Assert.NotNull(HandlerForeignKey(ctx));
        Assert.NotSame(AuthorForeignKey(ctx), HandlerForeignKey(ctx));
    }

    [Fact]
    public void AuthorForeignKey_TargetsApplicationUser()
    {
        // The author side is fine: it points at the same type the app creates.
        using var ctx = new TodoDbContext(TestDb.Options());

        Assert.Equal(typeof(ApplicationUser), AuthorForeignKey(ctx).PrincipalEntityType.ClrType);
    }

    [Fact]
    public void HandlerForeignKey_TargetsAdminUser_NotApplicationUser()
    {
        // CURRENT DESIGN: HandledByAdminUserId must match an AdminUser, which is
        // a *different* entity type from the ApplicationUser the seeder creates.
        // If this relationship is ever repointed at ApplicationUser, this test
        // should be updated - it documents the decision, it is not an endorsement.
        using var ctx = new TodoDbContext(TestDb.Options());

        Assert.Equal(typeof(AdminUser), HandlerForeignKey(ctx).PrincipalEntityType.ClrType);
        Assert.NotEqual(typeof(ApplicationUser), HandlerForeignKey(ctx).PrincipalEntityType.ClrType);
    }

    [Fact]
    public void AdminUser_IsADerivedEntityType_SoItIsNotJustAnApplicationUserRow()
    {
        // AdminUser inherits ApplicationUser, so being an admin is a property of
        // the *row's type*, not of a role assignment. That is what makes the
        // handler key unsatisfiable for a user created as ApplicationUser.
        using var ctx = new TodoDbContext(TestDb.Options());

        IEntityType adminType = ctx.Model.FindEntityType(typeof(AdminUser))!;
        IEntityType userType = ctx.Model.FindEntityType(typeof(ApplicationUser))!;

        Assert.NotNull(adminType);
        Assert.Same(userType, adminType.BaseType);
    }

    // ===== The runtime consequence =====

    [Fact]
    public async Task UserCreatedAsApplicationUser_IsNotAnAdminUser()
    {
        // This mirrors IdentitySeeder.AdminSeeder(), which calls
        // userManager.CreateAsync(new ApplicationUser { ... }).
        // The resulting row satisfies AspNetUsers, but NOT the AdminUser type
        // the handler foreign key requires.
        var options = TestDb.Options();
        Guid adminId = Guid.NewGuid();

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewPlainUser(adminId));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new TodoDbContext(options);

        Assert.True(await ctx.Users.AnyAsync(u => u.Id == adminId));
        Assert.False(await ctx.AdminUsers.AnyAsync(a => a.Id == adminId));
    }

    [Fact]
    public async Task HandlingWithAPlainApplicationUserId_LeavesTheHandlerUnresolvable()
    {
        // What HandleSupportMessaged(id, adminUserId) does, with the adminUserId
        // of a user created as ApplicationUser.
        //
        // On SQL Server this SaveChanges throws a FK violation (DbUpdateException),
        // because no AdminUsers row has that id. EF InMemory does not enforce
        // foreign keys, so here the save succeeds - but the damage is still
        // visible: the HandledByAdminUser navigation can never resolve, so
        // "handled by" is permanently null in every projection that reads it.
        var options = TestDb.Options();
        Guid adminId = Guid.NewGuid();
        Guid authorId = Guid.NewGuid();
        int messageId;

        await using (var seed = new TodoDbContext(options))
        {
            seed.Users.Add(NewPlainUser(adminId));
            seed.Users.Add(new ApplicationUser
            {
                Id = authorId,
                UserName = "john@example.com",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe"
            });

            SupportMessage message = new SupportMessage
            {
                Title = "Cannot delete a group",
                Description = "Pressing delete does nothing",
                CreatedOn = new DateTime(2026, 7, 20, 10, 0, 0),
                ApplicationUserId = authorId
            };

            seed.SupportMessages.Add(message);
            await seed.SaveChangesAsync();
            messageId = message.Id;
        }

        await using (var handle = new TodoDbContext(options))
        {
            SupportMessage message = await handle.SupportMessages.FirstAsync(m => m.Id == messageId);

            message.IsHandled = true;
            message.HandledByAdminUserId = adminId;
            message.HandledOn = new DateTime(2026, 7, 21, 9, 0, 0);

            await handle.SaveChangesAsync();
        }

        await using var verify = new TodoDbContext(options);
        SupportMessage handled = await verify.SupportMessages
            .IgnoreQueryFilters()
            .Include(m => m.HandledByAdminUser)
            .FirstAsync(m => m.Id == messageId);

        // The key was stored...
        Assert.Equal(adminId, handled.HandledByAdminUserId);
        // ...but it points at no AdminUser, so the handler is unknowable.
        Assert.Null(handled.HandledByAdminUser);
    }

    [Fact]
    public async Task HandlingWithARealAdminUserId_ResolvesTheHandler()
    {
        // The same flow succeeds when the admin was created AS an AdminUser -
        // which is exactly what the seeder does not do today. This is the
        // control case that proves the previous test fails for the right reason.
        var options = TestDb.Options();
        Guid adminId = Guid.NewGuid();
        Guid authorId = Guid.NewGuid();
        int messageId;

        await using (var seed = new TodoDbContext(options))
        {
            seed.AdminUsers.Add(new AdminUser
            {
                Id = adminId,
                UserName = "admin@todoapp.local",
                Email = "admin@todoapp.local",
                FirstName = "Velin",
                LastName = "Poznakov"
            });
            seed.Users.Add(new ApplicationUser
            {
                Id = authorId,
                UserName = "john@example.com",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe"
            });

            SupportMessage message = new SupportMessage
            {
                Title = "Cannot delete a group",
                Description = "Pressing delete does nothing",
                CreatedOn = new DateTime(2026, 7, 20, 10, 0, 0),
                ApplicationUserId = authorId
            };

            seed.SupportMessages.Add(message);
            await seed.SaveChangesAsync();
            messageId = message.Id;
        }

        await using (var handle = new TodoDbContext(options))
        {
            SupportMessage message = await handle.SupportMessages.FirstAsync(m => m.Id == messageId);

            message.IsHandled = true;
            message.HandledByAdminUserId = adminId;
            message.HandledOn = new DateTime(2026, 7, 21, 9, 0, 0);

            await handle.SaveChangesAsync();
        }

        await using var verify = new TodoDbContext(options);
        SupportMessage handled = await verify.SupportMessages
            .IgnoreQueryFilters()
            .Include(m => m.HandledByAdminUser)
            .FirstAsync(m => m.Id == messageId);

        Assert.NotNull(handled.HandledByAdminUser);
        Assert.Equal("Velin", handled.HandledByAdminUser!.FirstName);
    }
}
