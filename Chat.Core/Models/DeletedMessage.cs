namespace Chat.Core.Models;

public class DeletedMessage
{
    public string Id { get; set; } = null!; // This will be the original messageId
    public string ChatId { get; set; } = null!;
    public string DeletedBy { get; set; } = null!;
    public DateTime DeletedAt { get; set; }
}