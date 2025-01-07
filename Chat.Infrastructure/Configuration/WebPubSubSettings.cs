namespace Chat.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Azure Web PubSub service
/// </summary>
public class WebPubSubSettings
{
    public string ConnectionString { get; set; } = null!;
    public string Hub { get; set; } = "chat"; // Default hub name
}