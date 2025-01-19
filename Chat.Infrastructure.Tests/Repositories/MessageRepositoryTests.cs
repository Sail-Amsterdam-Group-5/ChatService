using Chat.Core.Models;
using Chat.Infrastructure.Data;
using Chat.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;
using FluentAssertions;

namespace Chat.Infrastructure.Tests.Repositories
{
    public class MessageRepositoryTests
    {
        private readonly Mock<Container> _containerMock;
        private readonly Mock<CosmosDbContext> _contextMock;
        private readonly MessageRepository _repository;

        public MessageRepositoryTests()
        {
            _containerMock = new Mock<Container>();
            _contextMock = new Mock<CosmosDbContext>();
            _contextMock.Setup(x => x.Messages).Returns(_containerMock.Object);
            _repository = new MessageRepository(_contextMock.Object);
        }

        [Fact]
        public async Task CreateMessageAsync_ValidMessage_ReturnsCreatedMessage()
        {
            // Arrange
            var message = new ChatMessage
            {
                ChatId = "chat1",
                SenderId = "user1",
                Type = "text",
                Content = new MessageContent { Text = "Hello" }
            };

            _containerMock.Setup(c => c.CreateItemAsync(
                It.IsAny<ChatMessage>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((ChatMessage m, PartitionKey pk, ItemRequestOptions o, CancellationToken t) =>
                {
                    var response = new Mock<ItemResponse<ChatMessage>>();
                    response.Setup(r => r.Resource).Returns(m);
                    return response.Object;
                });

            // Act
            var result = await _repository.CreateMessageAsync(message);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.ChatId.Should().Be("chat1");
            result.Type.Should().Be("text");
        }

        [Fact]
        public async Task GetMessageByIdAsync_ExistingMessage_ReturnsMessage()
        {
            // Arrange
            var messageId = "msg1";
            var chatId = "chat1";
            var message = new ChatMessage
            {
                Id = messageId,
                ChatId = chatId,
                Type = "text"
            };

            _containerMock.Setup(c => c.ReadItemAsync<ChatMessage>(
                messageId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, PartitionKey pk, ItemRequestOptions o, CancellationToken t) =>
                {
                    var response = new Mock<ItemResponse<ChatMessage>>();
                    response.Setup(r => r.Resource).Returns(message);
                    return response.Object;
                });

            // Act
            var result = await _repository.GetMessageByIdAsync(messageId, chatId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(messageId);
        }

        [Fact]
        public async Task DeleteMessageAsync_ExistingMessage_ReturnsTrue()
        {
            // Arrange
            var messageId = "msg1";
            var chatId = "chat1";

            _containerMock.Setup(c => c.DeleteItemAsync<ChatMessage>(
                messageId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, PartitionKey pk, ItemRequestOptions o, CancellationToken t) =>
                {
                    var response = new Mock<ItemResponse<ChatMessage>>();
                    return response.Object;
                });

            // Act
            var result = await _repository.DeleteMessageAsync(messageId, chatId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetRecentMessagesAsync_ReturnsMessages()
        {
            // Arrange
            var chatId = "chat1";
            var messages = new List<ChatMessage>
            {
                new() { Id = "msg1", ChatId = chatId },
                new() { Id = "msg2", ChatId = chatId }
            };

            var feedIteratorMock = new Mock<FeedIterator<ChatMessage>>();
            feedIteratorMock.SetupSequence(f => f.HasMoreResults)
                .Returns(true)
                .Returns(false);

            var responseWrapper = new Mock<FeedResponse<ChatMessage>>();
            responseWrapper.Setup(r => r.GetEnumerator())
                .Returns(messages.GetEnumerator());

            feedIteratorMock.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseWrapper.Object);

            _containerMock.Setup(c => c.GetItemLinqQueryable<ChatMessage>(
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>(),
                It.IsAny<CosmosLinqSerializerOptions>()))
                .Returns((bool a, string b, QueryRequestOptions c, CosmosLinqSerializerOptions d) =>
                {
                    var queryableMock = new Mock<IOrderedQueryable<ChatMessage>>();
                    return queryableMock.Object;
                });

            _containerMock.Setup(c => c.GetItemQueryIterator<ChatMessage>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await _repository.GetRecentMessagesAsync(chatId, 2);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }
    }
}