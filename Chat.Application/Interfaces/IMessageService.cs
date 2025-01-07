using Chat.Application.DTOs;

namespace Chat.Application.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto, string senderId);
    Task<IEnumerable<MessageDto>> GetChatMessagesAsync(string chatId, int page = 1, int pageSize = 50);
    Task<MessageDto?> GetMessageByIdAsync(string messageId, string chatId);
    Task<bool> DeleteMessageAsync(string messageId, string chatId, string userId);
    Task<IEnumerable<MessageDto>> GetNewMessagesAsync(string chatId, DateTime lastSyncTimestamp);
    Task<IEnumerable<MessageDto>> GetRecentMessagesAsync(string chatId, int limit = 50);
    Task<IEnumerable<MessageDto>> GetMessagesBeforeDateAsync(string chatId, DateTime beforeDate, int limit = 50);
}