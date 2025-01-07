using Microsoft.AspNetCore.Http;

namespace Chat.Application.DTOs;

public class MessageDto
{
    public string Id { get; set; } = null!;
    public string ChatId { get; set; } = null!;
    public string SenderId { get; set; } = null!;
    public string Type { get; set; } = "text";
    public MessageContentDto Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class MessageContentDto
{
    public string? Text { get; set; }
    public string? ImageUrl { get; set; }
    public long? ImageSize { get; set; }
    public string? ImageMimeType { get; set; }
}

public class CreateMessageDto
{
    public string ChatId { get; set; } = null!;
    public string Type { get; set; } = "text";
    public MessageContentDto? Content { get; set; }
    public IFormFile? ImageFile { get; set; }
}