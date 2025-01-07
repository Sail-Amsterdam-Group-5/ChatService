using Chat.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure.Data;

public class CosmosDbContext
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;

    public Container Messages { get; private set; }
    public Container Chats { get; private set; }
    public Container Devices { get; private set; }
    public Container DeletedMessages { get; private set; }

    public CosmosDbContext(IOptions<CosmosDbSettings> settings)
    {
        _client = new CosmosClient(settings.Value.ConnectionString);
        _databaseName = settings.Value.DatabaseName;

        // Create database if it doesn't exist
        var database = _client.CreateDatabaseIfNotExistsAsync(_databaseName)
            .GetAwaiter()
            .GetResult()
            .Database;

        // Create containers with correct partition key paths
        Messages = database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = settings.Value.MessagesContainer,
                PartitionKeyPath = "/chatId"
            }).GetAwaiter().GetResult();

        Chats = database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = settings.Value.ChatsContainer,
                PartitionKeyPath = "/id"
            }).GetAwaiter().GetResult();

        Devices = database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = settings.Value.DevicesContainer,
                PartitionKeyPath = "/userId"
            }).GetAwaiter().GetResult();

        DeletedMessages = database.CreateContainerIfNotExistsAsync(
            settings.Value.DeletedMessagesContainer,
            "/chatId"
        ).GetAwaiter().GetResult();
    }
}