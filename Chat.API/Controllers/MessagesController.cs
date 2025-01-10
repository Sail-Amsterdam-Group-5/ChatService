using Chat.API.Authentication;
using Chat.Application.DTOs;
using Chat.Application.Exceptions;
using Chat.Application.Interfaces;
using Chat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IChatService _chatService;
    private readonly IDeletedMessageService _deletedMessageService;

    private string UserId => User.GetUserId();
    private bool IsAdmin => User.IsInRole("admin");
    private bool IsTeamLead => User.IsInRole("team-lead");

    public MessagesController(IMessageService messageService, IChatService chatService, IDeletedMessageService deletedMessageService)
    {
        _messageService = messageService;
        _chatService = chatService;
        _deletedMessageService = deletedMessageService;
    }

    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetChatMessages(
        string chatId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Verify user is a participant in the chat
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound("Chat not found");

            if (!chat.Participants.Any(p => p.UserId == UserId))
                return Forbid();

            var messages = await _messageService.GetChatMessagesAsync(chatId, page, pageSize);
            return Ok(messages);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedMessages([FromQuery] DateTime lastSyncTime)
    {
        try
        {
            var deletedMessageIds = await _deletedMessageService.GetDeletedMessagesAfterAsync(lastSyncTime);
            return Ok(new { deletedMessages = deletedMessageIds });
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [Consumes("application/json", "multipart/form-data")]
    public async Task<IActionResult> SendMessage([FromForm] CreateMessageDto createMessageDto)
    {
        try
        {
            var chat = await _chatService.GetChatByIdAsync(createMessageDto.ChatId);
            if (chat == null)
                return NotFound("Chat not found");

            if (!chat.Participants.Any(p => p.UserId == UserId))
                return Forbid();

            // Validate based on message type
            if (createMessageDto.Type == "text" && string.IsNullOrEmpty(createMessageDto.Content?.Text))
            {
                return BadRequest("Text message cannot be empty");
            }
            else if (createMessageDto.Type == "image" && createMessageDto.ImageFile == null)
            {
                return BadRequest("Image file is required for image messages");
            }

            var message = await _messageService.SendMessageAsync(createMessageDto, UserId);
            return CreatedAtAction(
                nameof(GetChatMessages),
                new { chatId = message.ChatId },
                message);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(string messageId, [FromQuery] string chatId)
    {
        try
        {
            if (string.IsNullOrEmpty(chatId))
                return BadRequest("ChatId is required");

            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound("Chat not found");

            // Check if user can delete messages
            var canDelete = IsAdmin || IsTeamLead || chat.Participants.Any(p => p.UserId == UserId && p.Role == "admin");
            if (!canDelete)
            {
                // Regular users can only delete their own messages
                var message = await _messageService.GetMessageByIdAsync(messageId, chatId);
                if (message?.SenderId != UserId)
                {
                    return Forbid("You can only delete your own messages");
                }
            }

            var success = await _messageService.DeleteMessageAsync(messageId, chatId, UserId);
            return success ? Ok() : BadRequest("Failed to delete message");
        }
        catch (ChatException ex) when (ex is MessageNotFoundException)
        {
            return NotFound(ex.Message);
        }
        catch (ChatException ex) when (ex is UnauthorizedChatAccessException)
        {
            return Forbid();
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("metrics/system")]
    [Authorize(Roles = "admin")] // Updated to match Keycloak role
    public async Task<IActionResult> GetSystemMetrics()
    {
        // TODO: Implement metrics collection
        return Ok(new
        {
            message = "Metrics functionality will be implemented later"
        });
    }

    [HttpGet("{chatId}/sync")]
    public async Task<IActionResult> GetNewMessages(
        string chatId,
        [FromQuery] DateTime lastSyncTimestamp)
    {
        try
        {
            // Verify user is a participant in the chat
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound("Chat not found");

            if (!chat.Participants.Any(p => p.UserId == UserId))
                return Forbid();

            var messages = await _messageService.GetNewMessagesAsync(chatId, lastSyncTimestamp);
            return Ok(messages);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{chatId}/recent")]
    public async Task<IActionResult> GetRecentMessages(
        string chatId,
        [FromQuery] int limit = 50)
    {
        try
        {
            // Verify user is a participant in the chat
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound("Chat not found");

            if (!chat.Participants.Any(p => p.UserId == UserId))
                return Forbid();

            var messages = await _messageService.GetRecentMessagesAsync(chatId, Math.Min(limit, 100));
            return Ok(messages);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{chatId}/history")]
    public async Task<IActionResult> GetMessageHistory(
        string chatId,
        [FromQuery] DateTime beforeDate,
        [FromQuery] int limit = 50)
    {
        try
        {
            // Verify user is a participant in the chat
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound("Chat not found");

            if (!chat.Participants.Any(p => p.UserId == UserId))
                return Forbid();

            var messages = await _messageService.GetMessagesBeforeDateAsync(chatId, beforeDate, Math.Min(limit, 100));
            return Ok(messages);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}