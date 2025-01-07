using Chat.Application.DTOs;
using Chat.Core.Models;

namespace Chat.Application.Common;

public static class Mappings
{
    public static ChatDto ToDto(this ChatRoom chat)
    {
        return new ChatDto
        {
            Id = chat.Id,
            Type = chat.Type,
            Name = chat.Name,
            CreatedAt = chat.CreatedAt,
            CreatedBy = chat.CreatedBy,
            Participants = chat.Participants.Select(p => new ChatParticipantDto
            {
                UserId = p.UserId,
                Role = p.Role,
                JoinedAt = p.JoinedAt
            }).ToList(),
            LastMessageAt = chat.LastMessageAt,
            IsActive = chat.IsActive
        };
    }

    public static MessageDto ToDto(this ChatMessage message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            Type = message.Type,
            Content = new MessageContentDto
            {
                Text = message.Content.Text,
                ImageUrl = message.Content.ImageUrl,
                ImageSize = message.Content.ImageSize,
                ImageMimeType = message.Content.ImageMimeType
            },
            CreatedAt = message.CreatedAt
        };
    }
}