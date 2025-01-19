using Chat.Core.Models;
using Chat.Infrastructure.Data;
using Chat.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Chat.Infrastructure.Configuration;
using System.Linq.Expressions;

namespace Chat.Infrastructure.Tests.Repositories
{
    public class ChatRepositoryTests
    {
        private readonly Mock<Container> _containerMock;
        private readonly CosmosDbContext _context;
        private readonly ChatRepository _repository;

        public ChatRepositoryTests()
        {
            // Setup container mock
            _containerMock = new Mock<Container>();

            // Create context with mocked containers
            _context = new CosmosDbContext(
                new Mock<CosmosClient>().Object,
                new Mock<Container>().Object,
                _containerMock.Object,  // Chats container
                new Mock<Container>().Object,
                new Mock<Container>().Object
            );

            _repository = new ChatRepository(_context);
        }

        [Fact]
        public async Task CreateChatAsync_ValidChat_ReturnsCreatedChat()
        {
            // Arrange
            var chat = new ChatRoom
            {
                Type = "group",
                Name = "Test Group",
                CreatedBy = "user1",
                Participants = new List<ChatParticipant>
               {
                   new() { UserId = "user1", Role = "admin" }
               }
            };

            _containerMock.Setup(c => c.CreateItemAsync(
                It.IsAny<ChatRoom>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((ChatRoom c, PartitionKey pk, ItemRequestOptions o, CancellationToken t) =>
                {
                    var response = new Mock<ItemResponse<ChatRoom>>();
                    response.Setup(r => r.Resource).Returns(c);
                    return response.Object;
                });

            // Act
            var result = await _repository.CreateChatAsync(chat);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Type.Should().Be("group");
            result.Name.Should().Be("Test Group");
            result.CreatedBy.Should().Be("user1");
            result.Participants.Should().HaveCount(1);
            result.Participants.First().Role.Should().Be("admin");
        }

        [Fact]
        public async Task GetChatByIdAsync_ExistingChat_ReturnsChat()
        {
            // Arrange
            var chatId = "chat1";
            var chat = new ChatRoom
            {
                Id = chatId,
                Type = "group",
                Name = "Test Group"
            };

            _containerMock.Setup(c => c.ReadItemAsync<ChatRoom>(
                chatId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, PartitionKey pk, ItemRequestOptions o, CancellationToken t) =>
                {
                    var response = new Mock<ItemResponse<ChatRoom>>();
                    response.Setup(r => r.Resource).Returns(chat);
                    return response.Object;
                });

            // Act
            var result = await _repository.GetChatByIdAsync(chatId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(chatId);
            result.Type.Should().Be("group");
            result.Name.Should().Be("Test Group");
        }
    }
}