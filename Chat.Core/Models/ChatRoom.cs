using Newtonsoft.Json;

namespace Chat.Core.Models;

public class ChatRoom
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("type")]
    public string Type { get; set; } = null!; // "individual" or "group"

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("createdBy")]
    public string CreatedBy { get; set; } = null!;

    [JsonProperty("participants")]
    public List<ChatParticipant> Participants { get; set; } = new();

    [JsonProperty("lastMessageAt")]
    public DateTime LastMessageAt { get; set; }

    [JsonProperty("isActive")]
    public bool IsActive { get; set; }
}

public class ChatParticipant
{
    [JsonProperty("userId")]
    public string UserId { get; set; } = null!;

    [JsonProperty("role")]
    public string Role { get; set; } = null!; // "admin" or "member"

    [JsonProperty("joinedAt")]
    public DateTime JoinedAt { get; set; }
}