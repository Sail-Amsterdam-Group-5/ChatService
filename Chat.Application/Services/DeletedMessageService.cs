using Chat.Application.Common;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Core.Interfaces;
using Chat.Core.Models;

namespace Chat.Application.Services;

public class DeletedMessageService : IDeletedMessageService
{
    private readonly IDeletedMessageRepository _deletedMessageRepository;
    private readonly IWebPubSubService _webPubSubService;

    public DeletedMessageService(
        IDeletedMessageRepository deletedMessageRepository,
        IWebPubSubService webPubSubService)
    {
        _deletedMessageRepository = deletedMessageRepository;
        _webPubSubService = webPubSubService;
    }

    public async Task<bool> TrackDeletedMessageAsync(string messageId, string chatId, string deletedByUserId)
    {
        var deletedMessage = new DeletedMessage
        {
            Id = messageId,
            ChatId = chatId,
            DeletedBy = deletedByUserId,
            DeletedAt = DateTime.UtcNow
        };

        var success = await _deletedMessageRepository.AddDeletedMessageAsync(deletedMessage);

        if (success)
        {
            // Notify all chat participants through WebSocket
            await _webPubSubService.SendMessageToChatAsync(chatId, new
            {
                type = "message-deleted",
                data = new DeletedMessageDto
                {
                    MessageId = messageId,
                    ChatId = chatId,
                    DeletedBy = deletedByUserId,
                    DeletedAt = deletedMessage.DeletedAt
                }
            });
        }

        return success;
    }

    public async Task<IEnumerable<DeletedMessageDto>> GetDeletedMessagesAfterAsync(DateTime timestamp)
    {
        var deletedMessages = await _deletedMessageRepository.GetDeletedMessagesAfterAsync(timestamp);

        return deletedMessages.Select(dm => new DeletedMessageDto
        {
            MessageId = dm.Id,
            ChatId = dm.ChatId,
            DeletedBy = dm.DeletedBy,
            DeletedAt = dm.DeletedAt
        });
    }
}