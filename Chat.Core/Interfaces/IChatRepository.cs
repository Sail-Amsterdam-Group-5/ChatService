using Chat.Core.Models;

namespace Chat.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatRoom> CreateChatAsync(ChatRoom chat);
    Task<ChatRoom?> GetChatByIdAsync(string chatId);
    Task<IEnumerable<ChatRoom>> GetUserChatsAsync(string userId, string? type = null);
    Task<bool> AddParticipantAsync(string chatId, ChatParticipant participant);
    Task<bool> RemoveParticipantAsync(string chatId, string userId);
    Task<bool> UpdateParticipantRoleAsync(string chatId, string userId, string newRole);
    Task<bool> UpdateLastMessageTimeAsync(string chatId, DateTime lastMessageTime);
    Task<bool> SetChatActiveStatusAsync(string chatId, bool isActive);
}