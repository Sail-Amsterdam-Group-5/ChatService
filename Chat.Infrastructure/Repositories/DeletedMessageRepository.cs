using Chat.Core.Interfaces;
using Chat.Infrastructure.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Chat.Core.Models;

namespace Chat.Infrastructure.Repositories;

public class DeletedMessageRepository : IDeletedMessageRepository
{
    private readonly Container _container;

    public DeletedMessageRepository(CosmosDbContext dbContext)
    {
        _container = dbContext.DeletedMessages;
    }

    public async Task<bool> AddDeletedMessageAsync(DeletedMessage deletedMessage)
    {
        try
        {
            await _container.CreateItemAsync(
                deletedMessage,
                new PartitionKey(deletedMessage.ChatId));
            return true;
        }
        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task<IEnumerable<DeletedMessage>> GetDeletedMessagesAfterAsync(DateTime timestamp)
    {
        try
        {
            var query = _container.GetItemLinqQueryable<DeletedMessage>()
                .Where(m => m.DeletedAt > timestamp)
                .OrderBy(m => m.DeletedAt);

            var results = new List<DeletedMessage>();
            var iterator = query.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (CosmosException)
        {
            return Enumerable.Empty<DeletedMessage>();
        }
    }
}