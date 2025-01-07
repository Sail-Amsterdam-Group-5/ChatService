using Chat.Application.DTOs;

namespace Chat.Application.Interfaces;

public interface IDeletedMessageService
{
    Task<bool> TrackDeletedMessageAsync(string messageId, string chatId, string deletedByUserId);
    Task<IEnumerable<DeletedMessageDto>> GetDeletedMessagesAfterAsync(DateTime timestamp);
}