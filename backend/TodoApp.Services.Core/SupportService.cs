using System.Linq.Expressions;
using TodoApp.Data.Repositories.Contracts;
using TodoApp.GCommon.Exceptions;
using TodoApp.Models.Data;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core;

public class SupportService: ISupportService
{
    private readonly ISupportRepository _supportRepository;

    private Expression<Func<SupportMessage, SupportMessage>> _defaultProjection
        = s => new SupportMessage
        {
            Id = s.Id,
            Title = s.Title,
            CreatedOn = s.CreatedOn,
            ApplicationUser = new ApplicationUser
            {
                FirstName = s.ApplicationUser.FirstName,
                LastName = s.ApplicationUser.LastName
            },
            HandledByAdminUser = s.HandledByAdminUser == null
                ? null
                : new AdminUser
                {
                    FirstName = s.HandledByAdminUser.FirstName,
                    LastName = s.HandledByAdminUser.LastName
                },
            HandledOn = s.HandledOn
        };

    public SupportService(ISupportRepository supportRepository)
    {
        _supportRepository = supportRepository;
    }
    
    public async Task<IEnumerable<AllSupportMessagesDto>> GetAllSupportMessages()
    {
        IEnumerable<SupportMessage> supportMessages = await _supportRepository
            .GetAllSupportMessagesNoTracking(
                filter: null,
                projection: _defaultProjection,
                ignoreQueryFilter: true,
                includeUser: true);

        IEnumerable<AllSupportMessagesDto> result = supportMessages.Select(s => new AllSupportMessagesDto()
        {
            Id = s.Id,
            Title = s.Title,
            CreatedOn = s.CreatedOn,
            IssuedUserFullname = $"{s.ApplicationUser.FirstName} {s.ApplicationUser.LastName}",
            HandledOn = s.HandledOn,
            HandledByFullName = s.HandledByAdminUser == null
                ? null
                : $"{s.HandledByAdminUser.FirstName} {s.HandledByAdminUser.LastName}"
        });

        return result;
    }

    public async Task<IEnumerable<AllSupportMessagesDto>> GetAllUnhandledSupportMessages()
    {
        IEnumerable<SupportMessage> unhandledSupportMessages = await _supportRepository
            .GetAllSupportMessagesNoTracking(
                filter: null,
                projection: _defaultProjection,
                includeUser: true);
        
        IEnumerable<AllSupportMessagesDto> result = unhandledSupportMessages.Select(s => new AllSupportMessagesDto()
        {
            Id = s.Id,
            Title = s.Title,
            CreatedOn = s.CreatedOn,
            IssuedUserFullname = $"{s.ApplicationUser.FirstName} {s.ApplicationUser.LastName}",
            HandledOn = s.HandledOn,
            HandledByFullName = s.HandledByAdminUser == null
                ? null
                : $"{s.HandledByAdminUser.FirstName} {s.HandledByAdminUser.LastName}"
        });

        return result;
    }

    public async Task<IEnumerable<AllSupportMessagesDto>> GetAllHandledSupportMessages()
    {
        IEnumerable<SupportMessage> handledSupportMessages = await _supportRepository
            .GetAllSupportMessagesNoTracking(
                filter: s => s.IsHandled == true,
                projection: _defaultProjection,
                ignoreQueryFilter: true,
                includeUser: true);
        
        IEnumerable<AllSupportMessagesDto> result = handledSupportMessages.Select(s => new AllSupportMessagesDto()
        {
            Id = s.Id,
            Title = s.Title,
            CreatedOn = s.CreatedOn,
            IssuedUserFullname = $"{s.ApplicationUser.FirstName} {s.ApplicationUser.LastName}",
            HandledOn = s.HandledOn,
            HandledByFullName = s.HandledByAdminUser == null
                ? null
                : $"{s.HandledByAdminUser.FirstName} {s.HandledByAdminUser.LastName}"
        });

        return result;
    }

    public async Task<SupportMessageDetailsDto> GetSupportMessage(int id)
    {
        SupportMessage? supportMessage = await _supportRepository
            .GetSupportMessage(
                id,
                projection: s => new SupportMessage
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    CreatedOn = s.CreatedOn,
                    IsHandled = s.IsHandled,
                    ApplicationUser = new ApplicationUser()
                    {
                        FirstName = s.ApplicationUser.FirstName,
                        LastName = s.ApplicationUser.LastName
                    },
                    HandledByAdminUser = s.HandledByAdminUser == null
                        ? null
                        : new AdminUser()
                        {
                            FirstName = s.HandledByAdminUser.FirstName,
                            LastName = s.HandledByAdminUser.LastName
                        },
                    HandledOn = s.HandledOn
                },
                tracked: false,
                ignoreQueryFilter: true);
        
        if (supportMessage == null)
        {
            throw new EntityNotFoundException();
        }

        SupportMessageDetailsDto result = new SupportMessageDetailsDto()
        {
            Id = supportMessage.Id,
            Title = supportMessage.Title,
            Description = supportMessage.Description,
            CreatedOn = supportMessage.CreatedOn,
            IsHandled = supportMessage.IsHandled,
            IssuedUserFullName = $"{supportMessage.ApplicationUser.FirstName} {supportMessage.ApplicationUser.LastName}",
            HandledOn = supportMessage.HandledOn,
            HandledByUserFullName = supportMessage.HandledByAdminUser == null
                ? null
                : $"{supportMessage.HandledByAdminUser.FirstName} {supportMessage.HandledByAdminUser.LastName}"
        };

        return result;
    }

    public async Task<IEnumerable<AllSupportMessagesDto>> GetAllSupportMessagesForUser(Guid userId)
    {
        IEnumerable<SupportMessage> supportMessages = await _supportRepository
            .GetAllSupportMessagesNoTracking(
                filter: s => s.ApplicationUser.Id == userId,
                projection: _defaultProjection,
                ignoreQueryFilter: true);
        
        IEnumerable<AllSupportMessagesDto> result = supportMessages.Select(s => new AllSupportMessagesDto()
        {
            Id = s.Id,
            Title = s.Title,
            CreatedOn = s.CreatedOn,
            HandledOn = s.HandledOn,
            HandledByFullName = s.HandledByAdminUser == null
                ? null
                : $"{s.HandledByAdminUser.FirstName} {s.HandledByAdminUser.LastName}"
        });

        return result;
    }

    public async Task HandleSupportMessages(int supportMessageId, Guid adminUserId)
    {
        SupportMessage? supportMessage = await _supportRepository
            .GetSupportMessage(
                id: supportMessageId,
                projection: null);

        if (supportMessage == null)
        {
            throw new EntityNotFoundException();
        }

        supportMessage.IsHandled = true;
        supportMessage.HandledByAdminUserId = adminUserId;
        supportMessage.HandledOn = DateTime.Now;

        bool success = await _supportRepository.EditSupportMessage(supportMessage);

        if (!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task DeleteSupportMessage(int supportMessageId)
    {
        SupportMessage? supportMessage = await _supportRepository
            .GetSupportMessage(
                id: supportMessageId,
                projection: null,
                ignoreQueryFilter: true);

        if (supportMessage == null)
        {
            throw new EntityNotFoundException();
        }

        bool success = await _supportRepository.DeleteSupportMessage(supportMessage);

        if (!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task CreateSupportMessage(Guid userId, CreateSupportMessageDto dto)
    {
        bool success = await _supportRepository
            .CreateSupportMessage(new SupportMessage()
            {
                Title = dto.Title,
                Description = dto.Description,
                ApplicationUserId = userId,
                CreatedOn = DateTime.Now
            });

        if (!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task UnhandledSupportMessage(int id)
    {
        SupportMessage? supportMessage = await _supportRepository
            .GetSupportMessage(
                id: id,
                projection: null,
                ignoreQueryFilter: true);

        if (supportMessage == null)
        {
            throw new EntityNotFoundException();
        }
        
        supportMessage.IsHandled = false;
        supportMessage.HandledByAdminUserId = null;
        supportMessage.HandledOn = null;

        bool success = await _supportRepository.EditSupportMessage(supportMessage);

        if (!success)
        {
            throw new DataPersistFail();
        }
    }

    public async Task<IEnumerable<AllSupportMessagesDto>> GetAllUnhandledSupportMessagesForUser(Guid userId)
    {
        IEnumerable<SupportMessage> supportMessages = await _supportRepository
            .GetAllSupportMessagesNoTracking(
                filter: s => s.ApplicationUser.Id == userId,
                projection: _defaultProjection);
        
        IEnumerable<AllSupportMessagesDto> result = supportMessages.Select(s => new AllSupportMessagesDto()
        {
            Id = s.Id,
            Title = s.Title,
            CreatedOn = s.CreatedOn,
            HandledOn = s.HandledOn,
            HandledByFullName = s.HandledByAdminUser == null
                ? null
                : $"{s.HandledByAdminUser.FirstName} {s.HandledByAdminUser.LastName}"
        });

        return result;
    }

    public async Task<IEnumerable<AllSupportMessagesDto>> GetAllHandledSupportMessagesForUser(Guid userId)
    {
        IEnumerable<SupportMessage> supportMessages = await _supportRepository
            .GetAllSupportMessagesNoTracking(
                filter: s => s.ApplicationUser.Id == userId
                    && s.IsHandled == true,
                projection: _defaultProjection,
                ignoreQueryFilter: true);
        
        IEnumerable<AllSupportMessagesDto> result = supportMessages.Select(s => new AllSupportMessagesDto()
        {
            Id = s.Id,
            Title = s.Title,
            CreatedOn = s.CreatedOn,
            HandledOn = s.HandledOn,
            HandledByFullName = s.HandledByAdminUser == null
                ? null
                : $"{s.HandledByAdminUser.FirstName} {s.HandledByAdminUser.LastName}"
        });

        return result;
    }
}