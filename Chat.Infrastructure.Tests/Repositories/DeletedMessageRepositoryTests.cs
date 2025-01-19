// DeletedMessageRepositoryTests.cs
using Chat.Core.Models;
using Chat.Infrastructure.Data;
using Chat.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;
using FluentAssertions;

namespace Chat.Infrastructure.Tests.Repositories
{
    public class DeletedMessageRepositoryTests
    {
        private readonly Mock<Container> _containerMock;
        private readonly Mock<CosmosDbContext> _contextMock;
        private readonly DeletedMessageRepository _repository;

        public DeletedMessageRepositoryTests()
        {
            _containerMock = new Mock<Container>();
            _contextMock = new Mock<CosmosDbContext>();
            _contextMock.Setup(x => x.DeletedMessages).Returns(_containerMock.Object);
            _repository = new DeletedMessageRepository(_contextMock.Object);
        }

        [Fact]
        public async Task AddDeletedMessageAsync_ValidMessage_ReturnsTrue()
        {
            // Arrange
            var deletedMessage = new DeletedMessage
            {
                Id = "msg1",
                ChatId = "chat1",
                DeletedBy = "user1",
                DeletedAt = DateTime.UtcNow
            };

            _containerMock.Setup(c => c.CreateItemAsync(
                It.IsAny<DeletedMessage>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((DeletedMessage m, PartitionKey pk, ItemRequestOptions o, CancellationToken t) =>
                {
                    var response = new Mock<ItemResponse<DeletedMessage>>();
                    response.Setup(r => r.Resource).Returns(m);
                    return response.Object;
                });

            // Act
            var result = await _repository.AddDeletedMessageAsync(deletedMessage);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetDeletedMessagesAfterAsync_ReturnsMessages()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.AddHours(-1);
            var deletedMessages = new List<DeletedMessage>
            {
                new() { Id = "msg1", ChatId = "chat1", DeletedAt = DateTime.UtcNow },
                new() { Id = "msg2", ChatId = "chat2", DeletedAt = DateTime.UtcNow }
            };

            var feedIteratorMock = new Mock<FeedIterator<DeletedMessage>>();
            feedIteratorMock.SetupSequence(f => f.HasMoreResults)
                .Returns(true)
                .Returns(false);

            var responseWrapper = new Mock<FeedResponse<DeletedMessage>>();
            responseWrapper.Setup(r => r.GetEnumerator())
                .Returns(deletedMessages.GetEnumerator());

            feedIteratorMock.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseWrapper.Object);

            _containerMock.Setup(c => c.GetItemLinqQueryable<DeletedMessage>(
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>(),
                It.IsAny<CosmosLinqSerializerOptions>()))
                .Returns((bool a, string b, QueryRequestOptions c, CosmosLinqSerializerOptions d) =>
                {
                    var queryableMock = new Mock<IOrderedQueryable<DeletedMessage>>();
                    return queryableMock.Object;
                });

            _containerMock.Setup(c => c.GetItemQueryIterator<DeletedMessage>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await _repository.GetDeletedMessagesAfterAsync(timestamp);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }
    }
}