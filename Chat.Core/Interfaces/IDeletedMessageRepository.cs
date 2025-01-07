using Chat.Core.Models;

namespace Chat.Core.Interfaces;

public interface IDeletedMessageRepository
{
    Task<bool> AddDeletedMessageAsync(DeletedMessage deletedMessage);
    Task<IEnumerable<DeletedMessage>> GetDeletedMessagesAfterAsync(DateTime timestamp);
}