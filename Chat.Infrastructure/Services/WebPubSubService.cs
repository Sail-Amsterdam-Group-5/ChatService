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
}