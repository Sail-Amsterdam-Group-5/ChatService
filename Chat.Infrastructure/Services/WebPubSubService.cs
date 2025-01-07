using Azure.Core;
using Azure.Messaging.WebPubSub;
using Chat.Core.Interfaces;
using Chat.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Chat.Infrastructure.Services;

/// <summary>
/// Implementation of Web PubSub service using Azure.Messaging.WebPubSub
/// </summary>
public class WebPubSubService : IWebPubSubService
{
    private readonly WebPubSubServiceClient _serviceClient;
    private readonly string _hubName;
    private readonly ILogger<WebPubSubService> _logger;

    public WebPubSubService(IOptions<WebPubSubSettings> settings, ILogger<WebPubSubService> logger)
    {
        _serviceClient = new WebPubSubServiceClient(settings.Value.ConnectionString, settings.Value.Hub);
        _hubName = settings.Value.Hub;
        _logger = logger;
    }

    public async Task<string> GetClientConnectionUrlAsync(string userId)
    {
        try
        {
            // Get token with necessary permissions
            var uri = await _serviceClient.GetClientAccessUriAsync(
                TimeSpan.FromHours(1),
                userId,
                ["webpubsub.sendToGroup", "webpubsub.joinLeaveGroup"]);  // Roles/Permissions

            _logger.LogInformation("Generated WebPubSub URL for user {UserId} with permissions", userId);
            return uri.AbsoluteUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WebPubSub URL for user {UserId}", userId);
            throw;
        }
    }

    public async Task SendMessageToChatAsync(string chatId, object message)
    {
        try
        {
            _logger.LogInformation("Sending message to chat {ChatId}", chatId);
            await _serviceClient.SendToGroupAsync(
                chatId,
                RequestContent.Create(message),
                ContentType.ApplicationJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to chat {ChatId}", chatId);
            throw;
        }
    }

    public async Task AddUserToChatsAsync(string userId, IEnumerable<string> chatIds)
    {
        try
        {
            _logger.LogInformation("Adding user {UserId} to {Count} chats", userId, chatIds.Count());

            foreach (var chatId in chatIds)
            {
                try
                {
                    await _serviceClient.AddUserToGroupAsync(chatId, userId);
                    _logger.LogInformation("Added user {UserId} to chat {ChatId}", userId, chatId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add user {UserId} to chat {ChatId}", userId, chatId);
                    // Continue with other chats even if one fails
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to chats", userId);
            throw;
        }
    }

    public async Task SendMessageToUserAsync(string userId, object message)
    {
        try
        {
            _logger.LogInformation("Sending message to user {UserId}", userId);
            await _serviceClient.SendToUserAsync(
                userId,
                RequestContent.Create(message),
                ContentType.ApplicationJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to user {UserId}", userId);
            throw;
        }
    }

    public async Task AddUserToChatGroupAsync(string userId, string chatId)
    {
        try
        {
            _logger.LogInformation("Adding user {UserId} to chat {ChatId}", userId, chatId);
            await _serviceClient.AddUserToGroupAsync(chatId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to chat {ChatId}", userId, chatId);
            throw;
        }
    }

    public async Task RemoveUserFromChatGroupAsync(string userId, string chatId)
    {
        try
        {
            _logger.LogInformation("Removing user {UserId} from chat {ChatId}", userId, chatId);
            await _serviceClient.RemoveUserFromGroupAsync(chatId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from chat {ChatId}", userId, chatId);
            throw;
        }
    }
}