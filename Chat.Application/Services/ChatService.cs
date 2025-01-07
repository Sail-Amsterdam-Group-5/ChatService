using Chat.Application.Common;
using Chat.Application.DTOs;
using Chat.Application.Exceptions;
using Chat.Application.Interfaces;
using Chat.Core.Interfaces;
using Chat.Core.Models;

namespace Chat.Application.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IWebPubSubService _webPubSubService;

    public ChatService(IChatRepository chatRepository, IWebPubSubService webPubSubService)
    {
        _chatRepository = chatRepository;
        _webPubSubService = webPubSubService;
    }

    public async Task<ChatDto> CreateGroupChatAsync(CreateChatDto createChatDto, string creatorId)
    {
        if (createChatDto.Type != "group")
        {
            throw new InvalidOperationChatException("Invalid chat type for group chat creation.");
        }

        if (string.IsNullOrEmpty(createChatDto.Name))
        {
            throw new InvalidOperationChatException("Group chat name is required.");
        }

        var chatRoom = new ChatRoom
        {
            Type = "group",
            Name = createChatDto.Name,
            CreatedBy = creatorId,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = creatorId, Role = "admin", JoinedAt = DateTime.UtcNow }
            }
        };

        // Add other participants
        foreach (var participantId in createChatDto.ParticipantIds.Where(id => id != creatorId))
        {
            chatRoom.Participants.Add(new ChatParticipant
            {
                UserId = participantId,
                Role = "member",
                JoinedAt = DateTime.UtcNow
            });
        }

        var createdChat = await _chatRepository.CreateChatAsync(chatRoom);

        // Add all participants to the WebPubSub group
        foreach (var participant in chatRoom.Participants)
        {
            await _webPubSubService.AddUserToChatGroupAsync(participant.UserId, createdChat.Id);
        }

        return createdChat.ToDto();
    }

    public async Task<ChatDto> CreateDirectMessageAsync(string userId, string otherUserId)
    {
        if (userId == otherUserId)
        {
            throw new InvalidOperationChatException("Cannot create direct message chat with yourself.");
        }

        var chatRoom = new ChatRoom
        {
            Type = "individual",
            CreatedBy = userId,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = userId, Role = "member", JoinedAt = DateTime.UtcNow },
                new() { UserId = otherUserId, Role = "member", JoinedAt = DateTime.UtcNow }
            }
        };

        var createdChat = await _chatRepository.CreateChatAsync(chatRoom);

        // Add both users to the WebPubSub group
        await _webPubSubService.AddUserToChatGroupAsync(userId, createdChat.Id);
        await _webPubSubService.AddUserToChatGroupAsync(otherUserId, createdChat.Id);

        return createdChat.ToDto();
    }

    public async Task<ChatDto?> GetChatByIdAsync(string chatId)
    {
        var chat = await _chatRepository.GetChatByIdAsync(chatId);
        return chat?.ToDto();
    }

    public async Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, string? type = null)
    {
        //var query = _chatRepository.GetItemLinqQueryable<ChatRoom>()
        //    .Where(c => c.Participants.Any(p => p.UserId == userId));

        //if (!string.IsNullOrEmpty(type))
        //{
        //    query = query.Where(c => c.Type == type);
        //}

        //var iterator = query.ToFeedIterator();
        //var chats = new List<ChatRoom>();

        //while (iterator.HasMoreResults)
        //{
        //    var response = await iterator.ReadNextAsync();
        //    chats.AddRange(response);
        //}

        //return chats
        //    .OrderByDescending(c => c.LastMessageAt)
        //    .Select(c => c.ToDto());
        var chats = await _chatRepository.GetUserChatsAsync(userId, type);
        return chats.OrderByDescending(c => c.LastMessageAt).Select(c => c.ToDto());
    }

    public async Task<bool> AddUserToChatAsync(string chatId, string userId, string role = "member")
    {
        var chat = await _chatRepository.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            throw new ChatNotFoundException(chatId);
        }

        if (chat.Type == "individual")
        {
            throw new InvalidOperationChatException("Cannot add users to individual chats.");
        }

        var participant = new ChatParticipant
        {
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        return await _chatRepository.AddParticipantAsync(chatId, participant);
    }

    public async Task<bool> RemoveUserFromChatAsync(string chatId, string userId)
    {
        var chat = await _chatRepository.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            throw new ChatNotFoundException(chatId);
        }

        if (chat.Type == "individual")
        {
            throw new InvalidOperationChatException("Cannot remove users from individual chats.");
        }

        return await _chatRepository.RemoveParticipantAsync(chatId, userId);
    }

    public async Task<bool> UpdateUserRoleAsync(string chatId, string userId, string newRole)
    {
        var chat = await _chatRepository.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            throw new ChatNotFoundException(chatId);
        }

        if (chat.Type == "individual")
        {
            throw new InvalidOperationChatException("Cannot update roles in individual chats.");
        }

        return await _chatRepository.UpdateParticipantRoleAsync(chatId, userId, newRole);
    }

    public async Task<bool> DeactivateChatAsync(string chatId)
    {
        var chat = await _chatRepository.GetChatByIdAsync(chatId);
        if (chat == null)
        {
            throw new ChatNotFoundException(chatId);
        }

        return await _chatRepository.SetChatActiveStatusAsync(chatId, false);
    }
}