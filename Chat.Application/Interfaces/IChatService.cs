using Chat.Application.DTOs;

namespace Chat.Application.Interfaces;

public interface IChatService
{
    Task<ChatDto> CreateGroupChatAsync(CreateChatDto createChatDto, string creatorId);
    Task<ChatDto> CreateDirectMessageAsync(string userId, string otherUserId);
    Task<ChatDto?> GetChatByIdAsync(string chatId);
    Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, string? type = null);
    Task<bool> AddUserToChatAsync(string chatId, string userId, string role = "member");
    Task<bool> RemoveUserFromChatAsync(string chatId, string userId);
    Task<bool> UpdateUserRoleAsync(string chatId, string userId, string newRole);
    Task<bool> DeactivateChatAsync(string chatId);
}