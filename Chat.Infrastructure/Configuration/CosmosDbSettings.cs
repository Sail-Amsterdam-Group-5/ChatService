namespace Chat.Infrastructure.Configuration;

public class CosmosDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string MessagesContainer { get; set; } = null!;
    public string ChatsContainer { get; set; } = null!;
    public string DevicesContainer { get; set; } = null!;
    public string DeletedMessagesContainer { get; set; } = null!;
}