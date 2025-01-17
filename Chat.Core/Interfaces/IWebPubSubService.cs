namespace Chat.Core.Interfaces;

/// <summary>
/// Interface for Web PubSub service operations
/// </summary>
public interface IWebPubSubService
{
    /// <summary>
    /// Gets a connection URL for the WebSocket client
    /// </summary>
    /// <param name="userId">The ID of the user requesting connection</param>
    /// <returns>Connection URL with access token</returns>
    Task<string> GetClientConnectionUrlAsync(string userId);

    /// <summary>
    /// Sends a message to a specific chat room
    /// </summary>
    /// <param name="chatId">The ID of the chat room</param>
    /// <param name="message">The message to send</param>
    Task SendMessageToChatAsync(string chatId, object message);
}