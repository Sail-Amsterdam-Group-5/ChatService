using Chat.Application.Services;
using Chat.Core.Interfaces;
using Chat.Core.Models;
using Moq;
using Xunit;
using FluentAssertions;
using Chat.Application.DTOs;
using Chat.Application.Exceptions;
using Chat.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Chat.Application.Tests.Services
{
    public class ChatServiceTests
    {
        private readonly Mock<IMessageRepository> _messageRepositoryMock;
        private readonly Mock<IChatRepository> _chatRepositoryMock;
        private readonly Mock<IWebPubSubService> _webPubSubServiceMock;
        private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
        private readonly Mock<IDeletedMessageService> _deletedMessageServiceMock;
        private readonly Mock<MetricsService> _metricsServiceMock;
        private readonly ILogger<MessageService> _loggerMock;
        private readonly MessageService _messageService;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            _messageRepositoryMock = new Mock<IMessageRepository>();
            _chatRepositoryMock = new Mock<IChatRepository>();
            _webPubSubServiceMock = new Mock<IWebPubSubService>();
            _blobStorageServiceMock = new Mock<IBlobStorageService>();
            _deletedMessageServiceMock = new Mock<IDeletedMessageService>();
            _metricsServiceMock = new Mock<MetricsService>();
            _loggerMock = Mock.Of<ILogger<MessageService>>();

            _messageService = new MessageService(
                _messageRepositoryMock.Object,
                _chatRepositoryMock.Object,
                _webPubSubServiceMock.Object,
                _blobStorageServiceMock.Object,
                _deletedMessageServiceMock.Object,
                _metricsServiceMock.Object,
                _loggerMock);

            _chatService = new ChatService(
                _chatRepositoryMock.Object,
                _webPubSubServiceMock.Object);
        }

        [Fact]
        public async Task CreateGroupChat_WithValidData_ShouldReturnChatDto()
        {
            // Arrange
            var createChatDto = new CreateChatDto
            {
                Type = "group",
                Name = "Test Group",
                ParticipantIds = new List<string> { "user2", "user3" }
            };
            var creatorId = "user1";

            _chatRepositoryMock.Setup(x => x.CreateChatAsync(It.IsAny<ChatRoom>()))
                .ReturnsAsync((ChatRoom chat) => chat);

            // Act
            var result = await _chatService.CreateGroupChatAsync(createChatDto, creatorId);

            // Assert
            result.Should().NotBeNull();
            result.Type.Should().Be("group");
            result.Name.Should().Be("Test Group");
            result.CreatedBy.Should().Be(creatorId);
            result.Participants.Should().HaveCount(3);
            result.Participants.Should().Contain(p => p.UserId == creatorId && p.Role == "admin");
        }

        [Fact]
        public async Task CreateDirectMessage_WithValidUsers_ShouldReturnChatDto()
        {
            // Arrange
            var userId = "user1";
            var otherUserId = "user2";

            _chatRepositoryMock.Setup(x => x.CreateChatAsync(It.IsAny<ChatRoom>()))
                .ReturnsAsync((ChatRoom chat) => chat);

            // Act
            var result = await _chatService.CreateDirectMessageAsync(userId, otherUserId);

            // Assert
            result.Should().NotBeNull();
            result.Type.Should().Be("individual");
            result.Participants.Should().HaveCount(2);
            result.Participants.Should().Contain(p => p.UserId == userId && p.Role == "member");
            result.Participants.Should().Contain(p => p.UserId == otherUserId && p.Role == "member");
        }

        [Fact]
        public async Task CreateDirectMessage_WithSameUser_ShouldThrowException()
        {
            // Arrange
            var userId = "user1";

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationChatException>(() =>
                _chatService.CreateDirectMessageAsync(userId, userId));
        }

        [Fact]
        public async Task GetChatById_ExistingChat_ShouldReturnChatDto()
        {
            // Arrange
            var chatId = "chat1";
            var chatRoom = new ChatRoom
            {
                Id = chatId,
                Type = "group",
                Name = "Test Group",
                CreatedBy = "user1",
                Participants = new List<ChatParticipant>
                {
                    new() { UserId = "user1", Role = "admin", JoinedAt = DateTime.UtcNow }
                }
            };

            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chatRoom);

            // Act
            var result = await _chatService.GetChatByIdAsync(chatId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(chatId);
            result.Type.Should().Be("group");
            result.Name.Should().Be("Test Group");
        }

        [Fact]
        public async Task GetChatById_NonExistingChat_ShouldReturnNull()
        {
            // Arrange
            var chatId = "nonexistent";
            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync((ChatRoom)null);

            // Act
            var result = await _chatService.GetChatByIdAsync(chatId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserChats_ShouldReturnUserChats()
        {
            // Arrange
            var userId = "user1";
            var chatRooms = new List<ChatRoom>
            {
                new()
                {
                    Id = "chat1",
                    Type = "group",
                    Name = "Group 1",
                    CreatedBy = userId,
                    LastMessageAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new()
                {
                    Id = "chat2",
                    Type = "individual",
                    CreatedBy = userId,
                    LastMessageAt = DateTime.UtcNow
                }
            };

            _chatRepositoryMock.Setup(x => x.GetUserChatsAsync(userId, null))
                .ReturnsAsync(chatRooms);

            // Act
            var result = await _chatService.GetUserChatsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Id.Should().Be("chat2"); // Should be ordered by LastMessageAt
        }

        [Fact]
        public async Task AddUserToChat_GroupChat_ShouldSucceed()
        {
            // Arrange
            var chatId = "chat1";
            var userId = "user2";
            var chatRoom = new ChatRoom
            {
                Id = chatId,
                Type = "group",
                Name = "Test Group"
            };

            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chatRoom);
            _chatRepositoryMock.Setup(x => x.AddParticipantAsync(chatId, It.IsAny<ChatParticipant>()))
                .ReturnsAsync(true);

            // Act
            var result = await _chatService.AddUserToChatAsync(chatId, userId);

            // Assert
            result.Should().BeTrue();
            _chatRepositoryMock.Verify(x => x.AddParticipantAsync(chatId, It.Is<ChatParticipant>(
                p => p.UserId == userId && p.Role == "member")), Times.Once);
        }

        [Fact]
        public async Task AddUserToChat_IndividualChat_ShouldThrowException()
        {
            // Arrange
            var chatId = "chat1";
            var userId = "user2";
            var chatRoom = new ChatRoom
            {
                Id = chatId,
                Type = "individual"
            };

            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chatRoom);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationChatException>(() =>
                _chatService.AddUserToChatAsync(chatId, userId));
        }

        [Fact]
        public async Task UpdateUserRole_ToAdmin_ShouldSucceed()
        {
            // Arrange
            var chatId = "chat1";
            var userId = "user1";
            var chatRoom = new ChatRoom
            {
                Id = chatId,
                Type = "group",
                Name = "Test Group"
            };

            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chatRoom);
            _chatRepositoryMock.Setup(x => x.UpdateParticipantRoleAsync(chatId, userId, "admin"))
                .ReturnsAsync(true);

            // Act
            var result = await _chatService.UpdateUserRoleAsync(chatId, userId, "admin");

            // Assert
            result.Should().BeTrue();
            _chatRepositoryMock.Verify(x => x.UpdateParticipantRoleAsync(chatId, userId, "admin"), Times.Once);
        }

        [Fact]
        public async Task DeleteMessage_WithinTimeWindow_ShouldSucceed()
        {
            // Arrange
            var messageId = "msg1";
            var chatId = "chat1";
            var userId = "user1";
            var message = new ChatMessage
            {
                Id = messageId,
                ChatId = chatId,
                SenderId = userId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            _messageRepositoryMock.Setup(x => x.GetMessageByIdAsync(messageId, chatId))
                .ReturnsAsync(message);
            _messageRepositoryMock.Setup(x => x.DeleteMessageAsync(messageId, chatId))
                .ReturnsAsync(true);

            // Act
            var result = await _messageService.DeleteMessageAsync(messageId, chatId, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteMessage_OutsideTimeWindow_ShouldThrowException()
        {
            // Arrange
            var messageId = "msg1";
            var chatId = "chat1";
            var userId = "user1";
            var message = new ChatMessage
            {
                Id = messageId,
                ChatId = chatId,
                SenderId = userId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-20) // Outside 15-minute window
            };

            _messageRepositoryMock.Setup(x => x.GetMessageByIdAsync(messageId, chatId))
                .ReturnsAsync(message);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationChatException>(() =>
                _messageService.DeleteMessageAsync(messageId, chatId, userId));
        }

        [Fact]
        public async Task SendImageMessage_ValidImage_ShouldSucceed()
        {
            // Arrange
            var chatId = "chat1";
            var senderId = "user1";
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1000);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            var createMessageDto = new CreateMessageDto
            {
                ChatId = chatId,
                Type = "image",
                ImageFile = mockFile.Object
            };

            var chat = new ChatRoom
            {
                Id = chatId,
                IsActive = true,
                Participants = new List<ChatParticipant>
        {
            new() { UserId = senderId, Role = "member" }
        }
            };

            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chat);
            _messageRepositoryMock.Setup(x => x.CreateMessageAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync((ChatMessage msg) => msg);
            _blobStorageServiceMock.Setup(x => x.IsValidImage(It.IsAny<string>(), It.IsAny<long>()))
                .Returns(true);
            _blobStorageServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(("imageUrl", 1000, "image/jpeg"));

            // Act
            var result = await _messageService.SendMessageAsync(createMessageDto, senderId);

            // Assert
            result.Should().NotBeNull();
            result.Type.Should().Be("image");
            result.Content.ImageUrl.Should().NotBeNull();
        }

        [Fact]
        public async Task SendMessage_ToInactiveChat_ShouldThrowException()
        {
            // Arrange
            var chatId = "chat1";
            var senderId = "user1";
            var createMessageDto = new CreateMessageDto
            {
                ChatId = chatId,
                Type = "text",
                Content = new MessageContentDto { Text = "Hello" }
            };

            var chat = new ChatRoom
            {
                Id = chatId,
                IsActive = false,
                Participants = new List<ChatParticipant>
        {
            new() { UserId = senderId, Role = "member" }
        }
            };

            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chat);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationChatException>(() =>
                _messageService.SendMessageAsync(createMessageDto, senderId));
        }

        [Fact]
        public async Task RemoveUserFromChat_AsAdmin_ShouldSucceed()
        {
            // Arrange
            var chatId = "chat1";
            var adminId = "admin1";
            var userId = "user1";
            var chat = new ChatRoom
            {
                Id = chatId,
                Type = "group",
                Participants = new List<ChatParticipant>
        {
            new() { UserId = adminId, Role = "admin" }
        }
            };

            _chatRepositoryMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chat);
            _chatRepositoryMock.Setup(x => x.RemoveParticipantAsync(chatId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _chatService.RemoveUserFromChatAsync(chatId, userId);

            // Assert
            result.Should().BeTrue();
        }
    }
}