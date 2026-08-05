using System.Linq.Expressions;
using Moq;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Services.Core;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Tests;

// Unit tests for SupportService. ISupportRepository is mocked, so these cover
// the service's own mapping, the flags it passes down to the repository
// (filter / ignoreQueryFilter / includeUser) and failure translation.
public class SupportServiceTests
{
    private readonly Mock<ISupportRepository> _repo = new();

    private SupportService CreateService() => new SupportService(_repo.Object);

    private static SupportMessage SampleMessage(
        int id = 1,
        string title = "Cannot delete a group",
        bool isHandled = false,
        Guid? userId = null,
        bool withAdmin = false)
        => new SupportMessage
        {
            Id = id,
            Title = title,
            Description = "Full description of the issue",
            CreatedOn = new DateTime(2026, 7, 20, 10, 0, 0),
            IsHandled = isHandled,
            ApplicationUserId = userId ?? Guid.NewGuid(),
            ApplicationUser = new ApplicationUser { FirstName = "John", LastName = "Doe" },
            HandledOn = isHandled ? new DateTime(2026, 7, 21, 9, 0, 0) : null,
            HandledByAdminUser = withAdmin
                ? new AdminUser { FirstName = "Velin", LastName = "Poznakov" }
                : null
        };

    // Captures the arguments the service passes to the repository so the
    // filtering strategy of each list method can be asserted.
    private void SetupList(
        params SupportMessage[] messages)
        => _repo.Setup(r => r.GetAllSupportMessagesNoTracking(
                It.IsAny<Expression<Func<SupportMessage, bool>>?>(),
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(messages);

    // ===== GetAllSupportMessages (admin: everything) =====

    [Fact]
    public async Task GetAllSupportMessages_MapsFieldsAndIssuerName()
    {
        SetupList(SampleMessage());

        var service = CreateService();
        AllSupportMessagesDto dto = Assert.Single(await service.GetAllSupportMessages());

        Assert.Equal(1, dto.Id);
        Assert.Equal("Cannot delete a group", dto.Title);
        Assert.Equal(new DateTime(2026, 7, 20, 10, 0, 0), dto.CreatedOn);
        Assert.Equal("John Doe", dto.IssuedUserFullname);
        Assert.Null(dto.HandledByFullName);
    }

    [Fact]
    public async Task GetAllSupportMessages_IgnoresQueryFilter_SoHandledOnesAreIncluded()
    {
        SetupList(SampleMessage());

        var service = CreateService();
        await service.GetAllSupportMessages();

        _repo.Verify(r => r.GetAllSupportMessagesNoTracking(
            null,
            It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
            true,      // ignoreQueryFilter
            true),     // includeUser
            Times.Once);
    }

    [Fact]
    public async Task GetAllSupportMessages_MapsHandlingAdminName()
    {
        SetupList(SampleMessage(isHandled: true, withAdmin: true));

        var service = CreateService();
        AllSupportMessagesDto dto = Assert.Single(await service.GetAllSupportMessages());

        Assert.Equal("Velin Poznakov", dto.HandledByFullName);
        Assert.Equal(new DateTime(2026, 7, 21, 9, 0, 0), dto.HandledOn);
    }

    // ===== Unhandled / handled lists =====

    [Fact]
    public async Task GetAllUnhandledSupportMessages_LeavesQueryFilterOn()
    {
        // The global query filter is IsHandled == false, so the unhandled list
        // must NOT ignore it.
        SetupList(SampleMessage());

        var service = CreateService();
        await service.GetAllUnhandledSupportMessages();

        _repo.Verify(r => r.GetAllSupportMessagesNoTracking(
            null,
            It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
            false,     // ignoreQueryFilter stays off
            true),
            Times.Once);
    }

    [Fact]
    public async Task GetAllHandledSupportMessages_IgnoresFilterAndAsksForHandledOnly()
    {
        SetupList(SampleMessage(isHandled: true, withAdmin: true));

        var service = CreateService();
        await service.GetAllHandledSupportMessages();

        _repo.Verify(r => r.GetAllSupportMessagesNoTracking(
            It.Is<Expression<Func<SupportMessage, bool>>?>(f => f != null),
            It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
            true,
            true),
            Times.Once);
    }

    [Fact]
    public async Task GetAllHandledSupportMessages_FilterMatchesOnlyHandled()
    {
        Expression<Func<SupportMessage, bool>>? captured = null;
        _repo.Setup(r => r.GetAllSupportMessagesNoTracking(
                It.IsAny<Expression<Func<SupportMessage, bool>>?>(),
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Callback<Expression<Func<SupportMessage, bool>>?,
                      Expression<Func<SupportMessage, SupportMessage>>, bool, bool>(
                (f, _, _, _) => captured = f)
            .ReturnsAsync(Array.Empty<SupportMessage>());

        var service = CreateService();
        await service.GetAllHandledSupportMessages();

        Assert.NotNull(captured);
        Func<SupportMessage, bool> predicate = captured!.Compile();
        Assert.True(predicate(SampleMessage(isHandled: true)));
        Assert.False(predicate(SampleMessage(isHandled: false)));
    }

    // ===== Per-user lists =====

    [Fact]
    public async Task GetAllSupportMessagesForUser_FiltersByThatUser()
    {
        Guid userId = Guid.NewGuid();
        Expression<Func<SupportMessage, bool>>? captured = null;

        _repo.Setup(r => r.GetAllSupportMessagesNoTracking(
                It.IsAny<Expression<Func<SupportMessage, bool>>?>(),
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Callback<Expression<Func<SupportMessage, bool>>?,
                      Expression<Func<SupportMessage, SupportMessage>>, bool, bool>(
                (f, _, _, _) => captured = f)
            .ReturnsAsync(new[] { SampleMessage(userId: userId) });

        var service = CreateService();
        AllSupportMessagesDto dto = Assert.Single(await service.GetAllSupportMessagesForUser(userId));

        Assert.Equal(1, dto.Id);
        Assert.NotNull(captured);

        Func<SupportMessage, bool> predicate = captured!.Compile();
        SupportMessage mine = SampleMessage(userId: userId);
        mine.ApplicationUser.Id = userId;
        SupportMessage other = SampleMessage(userId: Guid.NewGuid());
        other.ApplicationUser.Id = Guid.NewGuid();

        Assert.True(predicate(mine));
        Assert.False(predicate(other));
    }

    [Fact]
    public async Task GetAllUnhandledSupportMessagesForUser_LeavesQueryFilterOn()
    {
        Guid userId = Guid.NewGuid();
        SetupList(SampleMessage(userId: userId));

        var service = CreateService();
        await service.GetAllUnhandledSupportMessagesForUser(userId);

        _repo.Verify(r => r.GetAllSupportMessagesNoTracking(
            It.Is<Expression<Func<SupportMessage, bool>>?>(f => f != null),
            It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
            false,
            false),
            Times.Once);
    }

    [Fact]
    public async Task GetAllHandledSupportMessagesForUser_IgnoresQueryFilter()
    {
        Guid userId = Guid.NewGuid();
        SetupList(SampleMessage(userId: userId, isHandled: true, withAdmin: true));

        var service = CreateService();
        AllSupportMessagesDto dto =
            Assert.Single(await service.GetAllHandledSupportMessagesForUser(userId));

        Assert.Equal("Velin Poznakov", dto.HandledByFullName);
        _repo.Verify(r => r.GetAllSupportMessagesNoTracking(
            It.Is<Expression<Func<SupportMessage, bool>>?>(f => f != null),
            It.IsAny<Expression<Func<SupportMessage, SupportMessage>>>(),
            true,
            false),
            Times.Once);
    }

    // ===== GetSupportMessage (details) =====

    [Fact]
    public async Task GetSupportMessage_MapsAllDetailFields()
    {
        _repo.Setup(r => r.GetSupportMessage(
                7,
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(SampleMessage(id: 7, isHandled: true, withAdmin: true));

        var service = CreateService();
        SupportMessageDetailsDto dto = await service.GetSupportMessage(7);

        Assert.Equal(7, dto.Id);
        Assert.Equal("Cannot delete a group", dto.Title);
        Assert.Equal("Full description of the issue", dto.Description);
        Assert.True(dto.IsHandled);
        Assert.Equal("John Doe", dto.IssuedUserFullName);
        Assert.Equal("Velin Poznakov", dto.HandledByUserFullName);
        Assert.Equal(new DateTime(2026, 7, 21, 9, 0, 0), dto.HandledOn);
    }

    [Fact]
    public async Task GetSupportMessage_WhenMissing_ThrowsEntityNotFound()
    {
        _repo.Setup(r => r.GetSupportMessage(
                It.IsAny<int>(),
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync((SupportMessage?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetSupportMessage(99));
    }

    // ===== CreateSupportMessage =====

    [Fact]
    public async Task CreateSupportMessage_BuildsEntityForTheUser()
    {
        Guid userId = Guid.NewGuid();
        SupportMessage? captured = null;

        _repo.Setup(r => r.CreateSupportMessage(It.IsAny<SupportMessage>()))
            .Callback<SupportMessage>(m => captured = m)
            .ReturnsAsync(true);

        var service = CreateService();
        await service.CreateSupportMessage(userId, new CreateSupportMessageDto
        {
            Title = "Broken button",
            Description = "The delete button does nothing"
        });

        Assert.NotNull(captured);
        Assert.Equal("Broken button", captured!.Title);
        Assert.Equal("The delete button does nothing", captured.Description);
        Assert.Equal(userId, captured.ApplicationUserId);
        Assert.False(captured.IsHandled);
        Assert.Null(captured.HandledOn);
        Assert.Null(captured.HandledByAdminUserId);
        Assert.NotEqual(default, captured.CreatedOn);
    }

    [Fact]
    public async Task CreateSupportMessage_WhenRepoFails_ThrowsDataPersistFail()
    {
        _repo.Setup(r => r.CreateSupportMessage(It.IsAny<SupportMessage>())).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(() => service.CreateSupportMessage(
            Guid.NewGuid(),
            new CreateSupportMessageDto { Title = "T", Description = "D" }));
    }

    // ===== HandleSupportMessaged =====

    [Fact]
    public async Task HandleSupportMessage_MarksHandledAndStampsAdminAndDate()
    {
        Guid adminId = Guid.NewGuid();
        SupportMessage message = SampleMessage();

        _repo.Setup(r => r.GetSupportMessage(
                message.Id,
                null,
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(message);
        _repo.Setup(r => r.EditSupportMessage(message)).ReturnsAsync(true);

        var service = CreateService();
        await service.HandleSupportMessaged(message.Id, adminId);

        Assert.True(message.IsHandled);
        Assert.Equal(adminId, message.HandledByAdminUserId);
        Assert.NotNull(message.HandledOn);
        _repo.Verify(r => r.EditSupportMessage(message), Times.Once);
    }

    [Fact]
    public async Task HandleSupportMessage_WhenMissing_ThrowsEntityNotFound()
    {
        _repo.Setup(r => r.GetSupportMessage(
                It.IsAny<int>(),
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync((SupportMessage?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.HandleSupportMessaged(1, Guid.NewGuid()));
    }

    [Fact]
    public async Task HandleSupportMessage_WhenSaveFails_ThrowsDataPersistFail()
    {
        SupportMessage message = SampleMessage();
        _repo.Setup(r => r.GetSupportMessage(
                message.Id, null, It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(message);
        _repo.Setup(r => r.EditSupportMessage(message)).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(
            () => service.HandleSupportMessaged(message.Id, Guid.NewGuid()));
    }

    // ===== UnhandledSupportMessage (re-open) =====

    [Fact]
    public async Task UnhandleSupportMessage_ClearsHandledStateAndIgnoresQueryFilter()
    {
        SupportMessage message = SampleMessage(isHandled: true, withAdmin: true);
        message.HandledByAdminUserId = Guid.NewGuid();

        _repo.Setup(r => r.GetSupportMessage(
                message.Id, null, It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(message);
        _repo.Setup(r => r.EditSupportMessage(message)).ReturnsAsync(true);

        var service = CreateService();
        await service.UnhandledSupportMessage(message.Id);

        Assert.False(message.IsHandled);
        Assert.Null(message.HandledByAdminUserId);
        Assert.Null(message.HandledOn);

        // A handled message is hidden by the global filter, so it must be
        // fetched with the filter ignored.
        _repo.Verify(r => r.GetSupportMessage(message.Id, null, It.IsAny<bool>(), true), Times.Once);
    }

    [Fact]
    public async Task UnhandleSupportMessage_WhenMissing_ThrowsEntityNotFound()
    {
        _repo.Setup(r => r.GetSupportMessage(
                It.IsAny<int>(),
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync((SupportMessage?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.UnhandledSupportMessage(5));
    }

    // ===== DeleteSupportMessage =====

    [Fact]
    public async Task DeleteSupportMessage_DeletesThroughRepository()
    {
        SupportMessage message = SampleMessage();

        _repo.Setup(r => r.GetSupportMessage(
                message.Id, null, It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(message);
        _repo.Setup(r => r.DeleteSupportMessage(message)).ReturnsAsync(true);

        var service = CreateService();
        await service.DeleteSupportMessage(message.Id);

        _repo.Verify(r => r.DeleteSupportMessage(message), Times.Once);
        // Handled messages must be deletable too, so the filter is ignored.
        _repo.Verify(r => r.GetSupportMessage(message.Id, null, It.IsAny<bool>(), true), Times.Once);
    }

    [Fact]
    public async Task DeleteSupportMessage_WhenMissing_ThrowsEntityNotFound()
    {
        _repo.Setup(r => r.GetSupportMessage(
                It.IsAny<int>(),
                It.IsAny<Expression<Func<SupportMessage, SupportMessage>>?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync((SupportMessage?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.DeleteSupportMessage(3));
    }

    [Fact]
    public async Task DeleteSupportMessage_WhenSaveFails_ThrowsDataPersistFail()
    {
        SupportMessage message = SampleMessage();
        _repo.Setup(r => r.GetSupportMessage(
                message.Id, null, It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(message);
        _repo.Setup(r => r.DeleteSupportMessage(message)).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<DataPersistFail>(
            () => service.DeleteSupportMessage(message.Id));
    }
}
