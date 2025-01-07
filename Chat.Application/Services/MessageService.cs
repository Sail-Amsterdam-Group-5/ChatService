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

    //public async Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto, string senderId)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Sending message from user {SenderId} to chat {ChatId}", senderId, createMessageDto.ChatId);

    //        // Verify chat exists and user is a participant
    //        var chat = await _chatRepository.GetChatByIdAsync(createMessageDto.ChatId);
    //        if (chat == null)
    //        {
    //            _logger.LogWarning("Chat {ChatId} not found", createMessageDto.ChatId);
    //            throw new ChatNotFoundException(createMessageDto.ChatId);
    //        }

    //        if (!chat.IsActive)
    //        {
    //            _logger.LogWarning("Attempted to send message to inactive chat {ChatId}", createMessageDto.ChatId);
    //            throw new InvalidOperationChatException("Cannot send messages to inactive chat.");
    //        }

    //        var participant = chat.Participants.FirstOrDefault(p => p.UserId == senderId);
    //        if (participant == null)
    //        {
    //            _logger.LogWarning("User {SenderId} is not a participant in chat {ChatId}", senderId, createMessageDto.ChatId);
    //            throw new UnauthorizedChatAccessException(senderId, createMessageDto.ChatId);
    //        }

    //        // Handle image upload if it's an image message
    //        if (createMessageDto.Type == "image")
    //        {
    //            _logger.LogInformation("Processing image message for chat {ChatId}", createMessageDto.ChatId);

    //            if (string.IsNullOrEmpty(createMessageDto.Content.ImageData))
    //            {
    //                throw new InvalidOperationChatException("Image data is required for image messages.");
    //            }

    //            // Upload image to blob storage
    //            var imageUrl = await _blobStorageService.UploadImageAsync(
    //                createMessageDto.Content.ImageData,
    //                createMessageDto.Content.ImageMimeType ?? "image/jpeg");

    //            createMessageDto.Content.ImageUrl = imageUrl;
    //            createMessageDto.Content.ImageData = null; // Clear the base64 data after upload
    //            _logger.LogInformation("Image uploaded successfully for chat {ChatId}", createMessageDto.ChatId);
    //        }

    //        // Create the message
    //        var message = new ChatMessage
    //        {
    //            ChatId = createMessageDto.ChatId,
    //            SenderId = senderId,
    //            Type = createMessageDto.Type,
    //            Content = new MessageContent
    //            {
    //                Text = createMessageDto.Content.Text,
    //                ImageUrl = createMessageDto.Content.ImageUrl,
    //                ImageSize = createMessageDto.Content.ImageSize,
    //                ImageMimeType = createMessageDto.Content.ImageMimeType
    //            }
    //        };

    //        var createdMessage = await _messageRepository.CreateMessageAsync(message);
    //        _logger.LogInformation("Message {MessageId} created in chat {ChatId}", createdMessage.Id, createMessageDto.ChatId);

    //        // Update chat's last message time
    //        await _chatRepository.UpdateLastMessageTimeAsync(createMessageDto.ChatId, createdMessage.CreatedAt);

    //        // Prepare WebSocket payload
    //        var webSocketPayload = new
    //        {
    //            type = "message",
    //            test = createdMessage.CreatedAt,
    //            data = new
    //            {
    //                id = createdMessage.Id,
    //                chatId = createdMessage.ChatId,
    //                senderId = createdMessage.SenderId,
    //                type = createdMessage.Type,
    //                content = createdMessage.Content,
    //                createdAt = createdMessage.CreatedAt
    //            }
    //        };

    //        // Serialize the payload as JSON
    //        string jsonPayload = JsonConvert.SerializeObject(webSocketPayload, Formatting.None);

    //        // Send real-time update to all chat participants
    //        try
    //        {
    //            await _webPubSubService.SendMessageToChatAsync(createMessageDto.ChatId, jsonPayload);
    //            _logger.LogInformation("Real-time message broadcast successful for message {MessageId} in chat {ChatId}",
    //                createdMessage.Id, createMessageDto.ChatId);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Failed to broadcast message {MessageId} to chat {ChatId}",
    //                createdMessage.Id, createMessageDto.ChatId);
    //            // Continue even if real-time broadcast fails
    //        }

    //        return createdMessage.ToDto();
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error sending message to chat {ChatId}", createMessageDto.ChatId);
    //        throw;
    //    }
    //}

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

    //public async Task<bool> DeleteMessageAsync(string messageId, string chatId, string userId)
    //{
    //    var message = await _messageRepository.GetMessageByIdAsync(messageId, chatId);
    //    if (message == null)
    //    {
    //        throw new MessageNotFoundException(messageId);
    //    }

    //    // Check 15-minute delete window
    //    var deleteWindow = DateTime.UtcNow.AddMinutes(-15);
    //    if (message.CreatedAt < deleteWindow)
    //    {
    //        throw new InvalidOperationChatException("Messages can only be deleted within 15 minutes of sending.");
    //    }

    //    // Check permissions
    //    var chat = await _chatRepository.GetChatByIdAsync(chatId);
    //    var isAdmin = chat?.Participants.Any(p => p.UserId == userId && p.Role == "admin") ?? false;
    //    if (message.SenderId != userId && !isAdmin)
    //    {
    //        throw new UnauthorizedChatAccessException(userId, chatId);
    //    }

    //    // Hard delete the message
    //    var success = await _messageRepository.DeleteMessageAsync(messageId, chatId);

    //    if (success)
    //    {
    //        // Track the deletion
    //        await _deletedMessageService.TrackDeletedMessageAsync(messageId, chatId, userId);
    //    }

    //    return success;
    //}

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