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

            var announceGroupId = Guid.NewGuid().ToString();
            var generalGroupId = Guid.NewGuid().ToString();
            var hospitalityGroupId = Guid.NewGuid().ToString();
            var dmChatId = Guid.NewGuid().ToString();

            var announceGroup = new ChatRoom
            {
                Id = announceGroupId,
                Type = "group",
                Name = "SAIL Announcements",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = USER_1,
                IsActive = true,
                LastMessageAt = DateTime.UtcNow,
                Participants = new List<ChatParticipant>
                {
                    new() { UserId = USER_1, Role = "admin", JoinedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { UserId = USER_3, Role = "member", JoinedAt = DateTime.UtcNow.AddDays(-10) }
                }
            };

            var generalGroup = new ChatRoom
            {
                Id = generalGroupId,
                Type = "group",
                Name = "SAIL General",
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                CreatedBy = USER_1,
                IsActive = true,
                LastMessageAt = DateTime.UtcNow,
                Participants = new List<ChatParticipant>
                {
                    new() { UserId = USER_1, Role = "admin", JoinedAt = DateTime.UtcNow.AddDays(-7) },
                    new() { UserId = USER_3, Role = "member", JoinedAt = DateTime.UtcNow.AddDays(-7) }
                }
            };

            var hospitalityGroup = new ChatRoom
            {
                Id = hospitalityGroupId,
                Type = "group",
                Name = "Hospitality workers",
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

            // Create the chats in the database
            var chatsContainer = database.Database.GetContainer(_chatsContainer);
            await chatsContainer.CreateItemAsync(announceGroup, new PartitionKey(announceGroup.Id));
            await chatsContainer.CreateItemAsync(generalGroup, new PartitionKey(generalGroup.Id));
            await chatsContainer.CreateItemAsync(hospitalityGroup, new PartitionKey(hospitalityGroup.Id));
            await chatsContainer.CreateItemAsync(dmChat, new PartitionKey(dmChat.Id));

            var messagesContainer = database.Database.GetContainer(_messagesContainer);

            // Sample messages for SAIL Announcements
            var announceMessages = new[]
            {
                ("Welcome to SAIL 2024! This channel will be used for important announcements.", USER_1),
                ("Important: Safety briefing tomorrow at 9:00 AM at the main dock.", USER_1),
                ("Weather update: Perfect sailing conditions expected this weekend!", USER_1),
                ("Reminder: All volunteers must check in at their designated posts 30 minutes before their shift.", USER_1)
            };

            // Sample messages for SAIL General
            var generalMessages = new[]
            {
                ("Hello everyone! Let's use this channel for general communication.", USER_1),
                ("Has anyone seen where the extra life jackets are stored?", USER_3),
                ("They're in the blue container near dock B", USER_1),
                ("Thanks! Found them", USER_3),
                ("What's the wifi password for the staff area?", USER_3),
                ("I'll send it to you in a DM", USER_1)
            };

            // Sample messages for Hospitality workers
            var hospitalityMessages = new[]
            {
                ("Welcome to the hospitality team channel!", USER_1),
                ("When does the first shift start tomorrow?", USER_3),
                ("First shift starts at 8:00 AM sharp", USER_1),
                ("Don't forget your name badges!", USER_1),
                ("Where do we pick up the new uniforms?", USER_3),
                ("At the staff center, between 9-5", USER_1)
            };

            // Sample messages for DM
            var dmMessages = new[]
            {
                ("Hey, got a minute to discuss the volunteer schedule?", USER_1),
                ("Sure, what's up?", USER_3),
                ("Can you cover the morning shift on Saturday?", USER_1),
                ("Yes, that works for me", USER_3),
                ("Great, thanks! I'll update the schedule", USER_1),
                ("No problem! Looking forward to it", USER_3)
            };

            // Helper function to create messages
            async Task CreateMessages(string chatId, (string message, string senderId)[] messages)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    var message = new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        ChatId = chatId,
                        SenderId = messages[i].senderId,
                        Type = "text",
                        Content = new MessageContent { Text = messages[i].message },
                        CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(i),
                        IsDeleted = false
                    };
                    await messagesContainer.CreateItemAsync(message, new PartitionKey(message.ChatId));
                }
            }

            // Create all messages
            await CreateMessages(announceGroupId, announceMessages);
            await CreateMessages(generalGroupId, generalMessages);
            await CreateMessages(hospitalityGroupId, hospitalityMessages);
            await CreateMessages(dmChatId, dmMessages);

            _logger.LogInformation("Database seeded successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }
}