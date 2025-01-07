namespace Chat.Application.DTOs;

public class ChatDto
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public List<ChatParticipantDto> Participants { get; set; } = new();
    public DateTime LastMessageAt { get; set; }
    public bool IsActive { get; set; }
}

public class ChatParticipantDto
{
    public string UserId { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
}

public class CreateChatDto
{
    public string Type { get; set; } = null!;
    public string? Name { get; set; }
    public List<string> ParticipantIds { get; set; } = new();
}