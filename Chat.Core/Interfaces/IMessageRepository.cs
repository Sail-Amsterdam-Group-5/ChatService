using Chat.Core.Models;

namespace Chat.Core.Interfaces;

public interface IMessageRepository
{
    Task<ChatMessage> CreateMessageAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetChatMessagesAsync(string chatId, int page = 1, int pageSize = 50);
    Task<ChatMessage?> GetMessageByIdAsync(string messageId, string chatId);
    Task<bool> DeleteMessageAsync(string messageId, string chatId);
    Task<IEnumerable<ChatMessage>> GetDeletedMessagesAfterAsync(DateTime timestamp);
    Task<IEnumerable<ChatMessage>> GetMessagesAfterTimestampAsync(string chatId, DateTime timestamp);
    Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(string chatId, int limit = 50);
    Task<IEnumerable<ChatMessage>> GetMessagesBeforeDateAsync(string chatId, DateTime beforeDate, int limit = 50);
}