using Chat.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Cosmos;
using Chat.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Chat.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly CosmosDbContext _dbContext;
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _chatsContainer;
    private readonly string _messagesContainer;
    private readonly ILogger<DatabaseSeeder> _logger;

    private const string USER_1 = "ab3f3f6d-621c-488c-8960-9c91397612f2";
    private const string USER_3 = "e1cb847d-e7af-4d52-9d7e-ff53b750dbd0";

    public DatabaseSeeder(
        CosmosDbContext dbContext,
        IOptions<CosmosDbSettings> settings,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _client = new CosmosClient(settings.Value.ConnectionString);
        _databaseName = settings.Value.DatabaseName;
        _chatsContainer = settings.Value.ChatsContainer;
        _messagesContainer = settings.Value.MessagesContainer;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Creating database if not exists: {DatabaseName}", _databaseName);
            var database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);

            _logger.LogInformation("Creating containers if not exist...");
            await database.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_chatsContainer, "/id")
                {
                    IndexingPolicy = new IndexingPolicy
                    {
                        IncludedPaths = { new IncludedPath { Path = "/*" } }
                    }
                });

            await database.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_messagesContainer, "/chatId")
                {
                    IndexingPolicy = new IndexingPolicy
                    {
                        IncludedPaths = { new IncludedPath { Path = "/*" } }
                    }
                });

            var groupChatId = Guid.NewGuid().ToString();
            var groupChat = new ChatRoom
            {
                Id = groupChatId,
                Type = "group",
                Name = "Test Group Chat",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CreatedBy = USER_1,
                IsActive = true,
                LastMessageAt = DateTime.UtcNow,
                Participants = new List<ChatParticipant>
               {
                   new() { UserId = USER_1, Role = "admin", JoinedAt = DateTime.UtcNow.AddDays(-5) },
                   new() { UserId = USER_3, Role = "member", JoinedAt = DateTime.UtcNow.AddDays(-5) }
               }
            };

            var dmChatId = Guid.NewGuid().ToString();
            var dmChat = new ChatRoom
            {
                Id = dmChatId,
                Type = "individual",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                CreatedBy = USER_1,
                IsActive = true,
                LastMessageAt = DateTime.UtcNow,
                Participants = new List<ChatParticipant>
               {
                   new() { UserId = USER_1, Role = "member", JoinedAt = DateTime.UtcNow.AddDays(-3) },
                   new() { UserId = USER_3, Role = "member", JoinedAt = DateTime.UtcNow.AddDays(-3) }
               }
            };

            _logger.LogInformation("Creating group chat...");
            await database.Database.GetContainer(_chatsContainer)
                .CreateItemAsync(groupChat, new PartitionKey(groupChat.Id));

            _logger.LogInformation("Creating DM chat...");
            await database.Database.GetContainer(_chatsContainer)
                .CreateItemAsync(dmChat, new PartitionKey(dmChat.Id));

            var messagesContainer = database.Database.GetContainer(_messagesContainer);

            // Generate 300 messages for group chat
            _logger.LogInformation("Creating 300 group messages...");
            for (int i = 1; i <= 300; i++)
            {
                var message = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = groupChatId,
                    SenderId = i % 2 == 0 ? USER_1 : USER_3,
                    Type = "text",
                    Content = new MessageContent { Text = $"Group Message #{i}" },
                    CreatedAt = DateTime.UtcNow.AddDays(-5).AddMinutes(i * 2),
                    IsDeleted = false
                };
                await messagesContainer.CreateItemAsync(message, new PartitionKey(message.ChatId));
            }

            // Generate 300 messages for DM chat
            _logger.LogInformation("Creating 300 DM messages...");
            for (int i = 1; i <= 300; i++)
            {
                var message = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = dmChatId,
                    SenderId = i % 2 == 0 ? USER_1 : USER_3,
                    Type = "text",
                    Content = new MessageContent { Text = $"DM Message #{i}" },
                    CreatedAt = DateTime.UtcNow.AddDays(-3).AddMinutes(i * 2),
                    IsDeleted = false
                };
                await messagesContainer.CreateItemAsync(message, new PartitionKey(message.ChatId));
            }

            _logger.LogInformation("Database seeded successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }
}