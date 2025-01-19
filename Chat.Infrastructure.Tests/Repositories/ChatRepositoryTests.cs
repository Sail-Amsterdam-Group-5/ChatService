using Chat.Core.Models;
using Chat.Infrastructure.Data;
using Chat.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Chat.Infrastructure.Configuration;

namespace Chat.Infrastructure.Tests.Repositories
{
    public class ChatRepositoryTests
    {
        private readonly Mock<Container> _containerMock;
        private readonly Mock<CosmosDbContext> _contextMock;
        private readonly ChatRepository _repository;
        private readonly Mock<FeedIterator<ChatRoom>> _feedIteratorMock;
        private readonly Mock<IQueryable<ChatRoom>> _queryableMock;

        public ChatRepositoryTests()
        {
            // Setup basic mocks
            _containerMock = new Mock<Container>();
            _contextMock = new Mock<CosmosDbContext>();
            _feedIteratorMock = new Mock<FeedIterator<ChatRoom>>();
            _queryableMock = new Mock<IQueryable<ChatRoom>>();

            // Setup context
            var contextMock = new Mock<CosmosDbContext>();
            contextMock.Setup(x => x.Chats).Returns(_containerMock.Object);

            // Setup queryable
            //_containerMock.Setup(c => c.GetItemLinqQueryable<ChatRoom>(
            //    It.IsAny<bool>(),
            //    It.IsAny<string>(),
            //    It.IsAny<QueryRequestOptions>(),
            //    It.IsAny<CosmosLinqSerializerOptions>()))
            //    .Returns((IOrderedQueryable<ChatRoom>)_queryableMock.Object);

            _repository = new ChatRepository(_contextMock.Object);
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

            var itemResponse = new Mock<ItemResponse<ChatRoom>>();
            itemResponse.Setup(r => r.Resource).Returns(chat);

            _containerMock.Setup(c => c.CreateItemAsync(
                It.IsAny<ChatRoom>(),
                It.IsAny<PartitionKey>(),
                null,
                default))
                .ReturnsAsync(itemResponse.Object);

            // Act
            var result = await _repository.CreateChatAsync(chat);

            // Assert
            result.Should().NotBeNull();
            result.Type.Should().Be("group");
            result.Name.Should().Be("Test Group");
        }
    }
}