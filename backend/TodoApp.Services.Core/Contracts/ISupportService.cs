using TodoApp.Services.Dtos;

namespace TodoApp.Services.Core.Contracts;

public interface ISupportService
{
    Task<IEnumerable<AllSupportMessagesDto>> GetAllSupportMessages();
    Task<IEnumerable<AllSupportMessagesDto>> GetAllUnhandledSupportMessages();
    Task<IEnumerable<AllSupportMessagesDto>> GetAllHandledSupportMessages();
    Task<SupportMessageDetailsDto> GetSupportMessage(int id);
    Task<IEnumerable<AllSupportMessagesDto>> GetAllSupportMessagesForUser(Guid userId);
    Task HandleSupportMessages(int supportMessageId, Guid adminUserId);
    Task DeleteSupportMessage(int supportMessageId);
    Task CreateSupportMessage(Guid userId, CreateSupportMessageDto dto);
    Task UnhandledSupportMessage(int id);
    Task<IEnumerable<AllSupportMessagesDto>> GetAllUnhandledSupportMessagesForUser(Guid userId);
    Task<IEnumerable<AllSupportMessagesDto>> GetAllHandledSupportMessagesForUser(Guid userId);
}