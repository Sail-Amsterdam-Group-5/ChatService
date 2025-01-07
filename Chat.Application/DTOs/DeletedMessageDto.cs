namespace Chat.Application.DTOs;

public class DeletedMessageDto
{
    public string MessageId { get; set; } = null!;
    public string ChatId { get; set; } = null!;
    public string DeletedBy { get; set; } = null!;
    public DateTime DeletedAt { get; set; }
}