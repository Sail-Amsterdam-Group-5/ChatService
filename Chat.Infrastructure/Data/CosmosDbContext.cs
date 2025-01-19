using Chat.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Chat.Infrastructure.Tests")] // Allow access to internal members for testing

namespace Chat.Infrastructure.Data;

public class CosmosDbContext
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _messagesContainer;
    private readonly string _chatsContainer;
    private readonly string _devicesContainer;
    private readonly string _deletedMessagesContainer;

    public Container Messages { get; private set; }
    public Container Chats { get; private set; }
    public Container Devices { get; private set; }
    public Container DeletedMessages { get; private set; }

    public CosmosDbContext(IOptions<CosmosDbSettings> settings)
    {
        _client = new CosmosClient(settings.Value.ConnectionString);
        _databaseName = settings.Value.DatabaseName;
        _messagesContainer = settings.Value.MessagesContainer;
        _chatsContainer = settings.Value.ChatsContainer;
        _devicesContainer = settings.Value.DevicesContainer;
        _deletedMessagesContainer = settings.Value.DeletedMessagesContainer;

        InitializeContainers().GetAwaiter().GetResult();
    }

    // Constructor for testing
    internal CosmosDbContext(
        CosmosClient client,
        Container messages,
        Container chats,
        Container devices,
        Container deletedMessages)
    {
        _client = client;
        Messages = messages;
        Chats = chats;
        Devices = devices;
        DeletedMessages = deletedMessages;
    }

    private async Task InitializeContainers()
    {
        var database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);

        Messages = await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = _messagesContainer,
                PartitionKeyPath = "/chatId"
            });

        Chats = await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = _chatsContainer,
                PartitionKeyPath = "/id"
            });

        Devices = await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = _devicesContainer,
                PartitionKeyPath = "/userId"
            });

        DeletedMessages = await database.Database.CreateContainerIfNotExistsAsync(
            _deletedMessagesContainer,
            "/chatId"
        );
    }
}