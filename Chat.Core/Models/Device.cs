using Newtonsoft.Json;

namespace Chat.Core.Models;

public class Device
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("userId")]
    public string UserId { get; set; } = null!;

    [JsonProperty("deviceToken")]
    public string DeviceToken { get; set; } = null!;

    [JsonProperty("platform")]
    public string Platform { get; set; } = null!;

    [JsonProperty("deviceId")]
    public string DeviceId { get; set; } = null!;

    [JsonProperty("registeredAt")]
    public DateTime RegisteredAt { get; set; }

    [JsonProperty("lastUpdated")]
    public DateTime LastUpdated { get; set; }
}