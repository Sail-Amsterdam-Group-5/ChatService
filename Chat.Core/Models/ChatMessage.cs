using Newtonsoft.Json;

namespace Chat.Core.Models;

public class ChatMessage
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("chatId")]
    public string ChatId { get; set; } = null!;

    [JsonProperty("senderId")]
    public string SenderId { get; set; } = null!;

    [JsonProperty("type")]
    public string Type { get; set; } = "text";

    [JsonProperty("content")]
    public MessageContent Content { get; set; } = null!;

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("isDeleted")]
    public bool IsDeleted { get; set; }
}

public class MessageContent
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonProperty("imageSize")]
    public long? ImageSize { get; set; }

    [JsonProperty("imageMimeType")]
    public string? ImageMimeType { get; set; }
}