using Chat.Core.Interfaces;
using Chat.Core.Models;
using Chat.Infrastructure.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Chat.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly Container _container;

    public MessageRepository(CosmosDbContext dbContext)
    {
        _container = dbContext.Messages;
    }

    public async Task<ChatMessage> CreateMessageAsync(ChatMessage message)
    {
        message.Id = Guid.NewGuid().ToString();
        message.CreatedAt = DateTime.UtcNow;
        return await _container.CreateItemAsync(message, new PartitionKey(message.ChatId));
    }

    public async Task<IEnumerable<ChatMessage>> GetChatMessagesAsync(string chatId, int page = 1, int pageSize = 50)
    {
        var query = _container.GetItemLinqQueryable<ChatMessage>()
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var iterator = query.ToFeedIterator();
        var messages = new List<ChatMessage>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            messages.AddRange(response);
        }

        return messages.OrderBy(m => m.CreatedAt);
    }

    public async Task<ChatMessage?> GetMessageByIdAsync(string messageId, string chatId)
    {
        try
        {
            var response = await _container.ReadItemAsync<ChatMessage>(
                messageId,
                new PartitionKey(chatId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteMessageAsync(string messageId, string chatId)
    {
        try
        {
            //var message = await GetMessageByIdAsync(messageId, chatId);
            //if (message == null) return false;

            //// Soft delete
            //message.IsDeleted = true;
            //await _container.UpsertItemAsync(message, new PartitionKey(chatId));
            //return true;
            // Hard delete - completely remove the message
            await _container.DeleteItemAsync<ChatMessage>(
                messageId,
                new PartitionKey(chatId));
            return true;
        }
        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task<IEnumerable<ChatMessage>> GetDeletedMessagesAfterAsync(DateTime timestamp)
    {
        var query = _container.GetItemLinqQueryable<ChatMessage>()
            .Where(m => m.IsDeleted && m.CreatedAt > timestamp);

        var iterator = query.ToFeedIterator();
        var messages = new List<ChatMessage>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            messages.AddRange(response);
        }

        return messages;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAfterTimestampAsync(string chatId, DateTime timestamp)
    {
        var query = _container.GetItemLinqQueryable<ChatMessage>()
            .Where(m => m.ChatId == chatId &&
                       m.CreatedAt > timestamp &&
                       !m.IsDeleted)
            .OrderBy(m => m.CreatedAt);

        var iterator = query.ToFeedIterator();
        var messages = new List<ChatMessage>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            messages.AddRange(response);
        }

        return messages;
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(string chatId, int limit = 50)
    {
        var query = _container.GetItemLinqQueryable<ChatMessage>()
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit);

        var iterator = query.ToFeedIterator();
        var messages = new List<ChatMessage>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            messages.AddRange(response);
        }

        return messages.OrderBy(m => m.CreatedAt);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBeforeDateAsync(string chatId, DateTime beforeDate, int limit = 50)
    {
        var query = _container.GetItemLinqQueryable<ChatMessage>()
            .Where(m => m.ChatId == chatId &&
                       m.CreatedAt < beforeDate &&
                       !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit);

        var iterator = query.ToFeedIterator();
        var messages = new List<ChatMessage>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            messages.AddRange(response);
        }

        return messages.OrderBy(m => m.CreatedAt);
    }
}