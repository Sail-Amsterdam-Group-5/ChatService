using Chat.Core.Interfaces;
using Chat.Core.Models;
using Chat.Infrastructure.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Chat.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly Container _container;

    public ChatRepository(CosmosDbContext dbContext)
    {
        _container = dbContext.Chats;
    }

    public async Task<ChatRoom> CreateChatAsync(ChatRoom chat)
    {
        chat.Id = Guid.NewGuid().ToString();
        chat.CreatedAt = DateTime.UtcNow;
        chat.LastMessageAt = DateTime.UtcNow;
        chat.IsActive = true;

        return await _container.CreateItemAsync(chat, new PartitionKey(chat.Id));
    }

    public async Task<ChatRoom?> GetChatByIdAsync(string chatId)
    {
        try
        {
            var response = await _container.ReadItemAsync<ChatRoom>(
                chatId,
                new PartitionKey(chatId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<ChatRoom>> GetUserChatsAsync(string userId, string? type = null)
    {
        try
        {
            var queryable = _container.GetItemLinqQueryable<ChatRoom>()
                .Where(c => c.Participants.Any(p => p.UserId == userId));

            if (!string.IsNullOrEmpty(type))
            {
                queryable = queryable.Where(c => c.Type == type);
            }
            //var queryable = _container.GetItemLinqQueryable<ChatRoom>()
            //    .Where(c => c.Participants.Any(p => p.UserId == userId));

            //if (!string.IsNullOrEmpty(type))
            //{
            //    queryable = queryable.Where(c => c.Type == type);
            //}

            var query = queryable.OrderByDescending(c => c.LastMessageAt);
            var iterator = query.ToFeedIterator();
            var chats = new List<ChatRoom>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                chats.AddRange(response);
            }

            return chats;
        }
        catch (CosmosException)
        {
            return Enumerable.Empty<ChatRoom>();
        }
    }

    public async Task<bool> AddParticipantAsync(string chatId, ChatParticipant participant)
    {
        try
        {
            var chat = await GetChatByIdAsync(chatId);
            if (chat == null) return false;

            if (!chat.Participants.Any(p => p.UserId == participant.UserId))
            {
                participant.JoinedAt = DateTime.UtcNow;
                chat.Participants.Add(participant);
                await _container.ReplaceItemAsync(chat, chatId, new PartitionKey(chatId));
            }
            return true;
        }
        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task<bool> RemoveParticipantAsync(string chatId, string userId)
    {
        try
        {
            var chat = await GetChatByIdAsync(chatId);
            if (chat == null) return false;

            var participant = chat.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant != null)
            {
                chat.Participants.Remove(participant);
                await _container.ReplaceItemAsync(chat, chatId, new PartitionKey(chatId));
            }
            return true;
        }
        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task<bool> UpdateParticipantRoleAsync(string chatId, string userId, string newRole)
    {
        try
        {
            var chat = await GetChatByIdAsync(chatId);
            if (chat == null) return false;

            var participant = chat.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant != null)
            {
                participant.Role = newRole;
                await _container.ReplaceItemAsync(chat, chatId, new PartitionKey(chatId));
                return true;
            }
            return false;
        }
        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task<bool> UpdateLastMessageTimeAsync(string chatId, DateTime lastMessageTime)
    {
        try
        {
            var chat = await GetChatByIdAsync(chatId);
            if (chat == null) return false;

            chat.LastMessageAt = lastMessageTime;
            await _container.ReplaceItemAsync(chat, chatId, new PartitionKey(chatId));
            return true;
        }
        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task<bool> SetChatActiveStatusAsync(string chatId, bool isActive)
    {
        try
        {
            var chat = await GetChatByIdAsync(chatId);
            if (chat == null) return false;

            chat.IsActive = isActive;
            await _container.ReplaceItemAsync(chat, chatId, new PartitionKey(chatId));
            return true;
        }
        catch (CosmosException)
        {
            return false;
        }
    }
}