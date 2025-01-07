using Chat.API.Authentication;
using Chat.Application.DTOs;
using Chat.Application.Exceptions;
using Chat.Application.Interfaces;
using Chat.Core.Interfaces;
using Chat.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IWebPubSubService _webPubSubService;
    private string UserId => User.GetUserId();
    private bool IsAdmin => User.IsInRole("admin");
    private bool IsTeamLead => User.IsInRole("team-lead");

    public ChatsController(IChatService chatService, IWebPubSubService webPubSubService)
    {
        _chatService = chatService;
        _webPubSubService = webPubSubService;
    }

    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetChat(string chatId)
    {
        try
        {
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound();

            // Verify user is a participant
            if (!chat.Participants.Any(p => p.UserId == UserId))
                return Forbid();

            return Ok(chat);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateChat(CreateChatDto createChatDto)
    {
        try
        {
            ChatDto chat;
            if (createChatDto.Type == "group")
            {
                chat = await _chatService.CreateGroupChatAsync(createChatDto, UserId);
            }
            else if (createChatDto.Type == "individual")
            {
                if (createChatDto.ParticipantIds.Count != 1)
                    return BadRequest("Individual chat must have exactly one other participant");

                chat = await _chatService.CreateDirectMessageAsync(UserId, createChatDto.ParticipantIds[0]);
            }
            else
            {
                return BadRequest("Invalid chat type");
            }

            return CreatedAtAction(nameof(GetChat), new { chatId = chat.Id }, chat);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserChats([FromQuery] string? type = null)
    {
        try
        {
            var chats = await _chatService.GetUserChatsAsync(UserId, type);
            return Ok(chats);
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{chatId}/users/{userId}")]
    public async Task<IActionResult> AddUserToChat(string chatId, string userId)
    {
        try
        {
            // Verify current user is admin
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound();

            if (!chat.Participants.Any(p => p.UserId == UserId && p.Role == "admin"))
                return Forbid();

            var success = await _chatService.AddUserToChatAsync(chatId, userId);
            return success ? Ok() : BadRequest("Failed to add user to chat");
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{chatId}/admins/{userId}")]
    public async Task<IActionResult> AddAdminToChat(string chatId, string userId)
    {
        try
        {
            // Verify current user is admin
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound();

            if (!chat.Participants.Any(p => p.UserId == UserId && p.Role == "admin"))
                return Forbid();

            var success = await _chatService.UpdateUserRoleAsync(chatId, userId, "admin");
            return success ? Ok() : BadRequest("Failed to update user role");
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{chatId}/users/{userId}")]
    public async Task<IActionResult> RemoveUserFromChat(string chatId, string userId)
    {
        try
        {
            // Verify current user is admin
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound();

            if (!chat.Participants.Any(p => p.UserId == UserId && p.Role == "admin"))
                return Forbid();

            var success = await _chatService.RemoveUserFromChatAsync(chatId, userId);
            return success ? Ok() : BadRequest("Failed to remove user from chat");
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{chatId}/admins/{userId}")]
    public async Task<IActionResult> RemoveAdminFromChat(string chatId, string userId)
    {
        try
        {
            // Verify current user is admin
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
                return NotFound();

            if (!chat.Participants.Any(p => p.UserId == UserId && p.Role == "admin"))
                return Forbid();

            var success = await _chatService.UpdateUserRoleAsync(chatId, userId, "member");
            return success ? Ok() : BadRequest("Failed to update user role");
        }
        catch (ChatException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}