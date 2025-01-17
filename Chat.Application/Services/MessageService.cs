using Chat.Application.Common;
using Chat.Application.DTOs;
using Chat.Application.Exceptions;
using Chat.Application.Interfaces;
using Chat.Core.Interfaces;
using Chat.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Chat.Application.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IWebPubSubService _webPubSubService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDeletedMessageService _deletedMessageService;
    private readonly ILogger<MessageService> _logger;
    private const int MESSAGE_DELETE_WINDOW_MINUTES = 5;

    public MessageService(
        IMessageRepository messageRepository,
        IChatRepository chatRepository,
        IWebPubSubService webPubSubService,
        IBlobStorageService blobStorageService,
        IDeletedMessageService deletedMessageService,
        ILogger<MessageService> logger)
    {
        _messageRepository = messageRepository;
        _chatRepository = chatRepository;
        _webPubSubService = webPubSubService;
        _blobStorageService = blobStorageService;
        _deletedMessageService = deletedMessageService;
        _logger = logger;
    }

    public async Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto, string senderId)
    {
        var chat = await _chatRepository.GetChatByIdAsync(createMessageDto.ChatId);
        if (chat == null)
            throw new ChatNotFoundException(createMessageDto.ChatId);

        if (!chat.IsActive)
            throw new InvalidOperationChatException("Cannot send messages to inactive chat.");

        var participant = chat.Participants.FirstOrDefault(p => p.UserId == senderId);
        if (participant == null)
            throw new UnauthorizedChatAccessException(senderId, createMessageDto.ChatId);

        var message = new ChatMessage
        {
            ChatId = createMessageDto.ChatId,
            SenderId = senderId,
            Type = createMessageDto.Type,
            Content = new MessageContent()
        };

        if (createMessageDto.Type == "image" && createMessageDto.ImageFile != null)
        {
            if (!_blobStorageService.IsValidImage(createMessageDto.ImageFile.ContentType, createMessageDto.ImageFile.Length))
            {
                throw new InvalidOperationChatException("Invalid image file. Must be jpg, png, or gif under 5MB.");
            }

            using var stream = createMessageDto.ImageFile.OpenReadStream();
            var (imageUrl, size, contentType) = await _blobStorageService.UploadImageAsync(
                stream,
                createMessageDto.ImageFile.ContentType,
                createMessageDto.ImageFile.FileName);

            message.Content.ImageUrl = imageUrl;
            message.Content.ImageSize = size;
            message.Content.ImageMimeType = contentType;
        }
        else if (createMessageDto.Type == "text")
        {
            message.Content.Text = createMessageDto.Content.Text;
        }
        else
        {
            throw new InvalidOperationChatException("Invalid message type");
        }

        var createdMessage = await _messageRepository.CreateMessageAsync(message);
        await _chatRepository.UpdateLastMessageTimeAsync(createMessageDto.ChatId, createdMessage.CreatedAt);

        await _webPubSubService.SendMessageToChatAsync(createMessageDto.ChatId, new
        {
            type = "message",
            data = createdMessage.ToDto()
        });

        return createdMessage.ToDto();
    }

    public async Task<IEnumerable<MessageDto>> GetNewMessagesAsync(string chatId, DateTime lastSyncTimestamp)
    {
        var messages = await _messageRepository.GetMessagesAfterTimestampAsync(chatId, lastSyncTimestamp);
        return messages.Select(m => m.ToDto());
    }

    public async Task<IEnumerable<MessageDto>> GetChatMessagesAsync(string chatId, int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Fetching messages for chat {ChatId} (Page {Page}, Size {PageSize})",
                chatId, page, pageSize);

            var messages = await _messageRepository.GetChatMessagesAsync(chatId, page, pageSize);
            return messages.Select(m => m.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching messages for chat {ChatId}", chatId);
            throw;
        }
    }

    public async Task<MessageDto?> GetMessageByIdAsync(string messageId, string chatId)
    {
        try
        {
            var message = await _messageRepository.GetMessageByIdAsync(messageId, chatId);
            return message?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching message {MessageId} from chat {ChatId}", messageId, chatId);
            throw;
        }
    }

    public async Task<bool> DeleteMessageAsync(string messageId, string chatId, string userId)
    {
        var message = await _messageRepository.GetMessageByIdAsync(messageId, chatId);
        if (message == null)
            throw new MessageNotFoundException(messageId);

        if (message.CreatedAt < DateTime.UtcNow.AddMinutes(-15))
            throw new InvalidOperationChatException("Messages can only be deleted within 15 minutes of sending.");

        var chat = await _chatRepository.GetChatByIdAsync(chatId);
        var isAdmin = chat?.Participants.Any(p => p.UserId == userId && p.Role == "admin") ?? false;
        if (message.SenderId != userId && !isAdmin)
            throw new UnauthorizedChatAccessException(userId, chatId);

        var success = await _messageRepository.DeleteMessageAsync(messageId, chatId);

        if (success && message.Type == "image" && !string.IsNullOrEmpty(message.Content.ImageUrl))
        {
            await _blobStorageService.DeleteImageAsync(message.Content.ImageUrl);
            await _webPubSubService.SendMessageToChatAsync(chatId, new
            {
                type = "message-deleted",
                data = new { messageId, chatId, deletedAt = DateTime.UtcNow }
            });
        }

        return success;
    }

    public async Task<IEnumerable<MessageDto>> GetRecentMessagesAsync(string chatId, int limit = 50)
    {
        var messages = await _messageRepository.GetRecentMessagesAsync(chatId, limit);
        return messages.Select(m => m.ToDto());
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesBeforeDateAsync(string chatId, DateTime beforeDate, int limit = 50)
    {
        var messages = await _messageRepository.GetMessagesBeforeDateAsync(chatId, beforeDate, limit);
        return messages.Select(m => m.ToDto());
    }
}