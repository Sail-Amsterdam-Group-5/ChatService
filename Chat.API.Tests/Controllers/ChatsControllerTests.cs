using Chat.API.Controllers;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace Chat.API.Tests.Controllers
{
    public class ChatsControllerTests
    {
        private readonly Mock<IChatService> _chatServiceMock;
        private readonly Mock<IWebPubSubService> _webPubSubServiceMock;
        private readonly ChatsController _controller;
        private readonly string _userId = "testUser";

        public ChatsControllerTests()
        {
            _chatServiceMock = new Mock<IChatService>();
            _webPubSubServiceMock = new Mock<IWebPubSubService>();

            // Setup controller with mock user
            _controller = new ChatsController(_chatServiceMock.Object, _webPubSubServiceMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId),
                new Claim(ClaimTypes.Role, "admin"),
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetChat_ExistingChat_ReturnsOkResult()
        {
            // Arrange
            var chatId = "chat123";
            var chatDto = new ChatDto
            {
                Id = chatId,
                Participants = new List<ChatParticipantDto>
                {
                    new() { UserId = _userId, Role = "admin" }
                }
            };

            _chatServiceMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chatDto);

            // Act
            var result = await _controller.GetChat(chatId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedChat = Assert.IsType<ChatDto>(okResult.Value);
            returnedChat.Id.Should().Be(chatId);
        }

        [Fact]
        public async Task GetChat_NonExistingChat_ReturnsNotFound()
        {
            // Arrange
            var chatId = "nonexistent";
            _chatServiceMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync((ChatDto)null);

            // Act
            var result = await _controller.GetChat(chatId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateChat_ValidGroupChat_ReturnsCreatedResult()
        {
            // Arrange
            var createChatDto = new CreateChatDto
            {
                Type = "group",
                Name = "Test Group",
                ParticipantIds = new List<string> { "user1", "user2" }
            };

            var createdChat = new ChatDto
            {
                Id = "newChat",
                Type = "group",
                Name = "Test Group"
            };

            _chatServiceMock.Setup(x => x.CreateGroupChatAsync(It.IsAny<CreateChatDto>(), It.IsAny<string>()))
                .ReturnsAsync(createdChat);

            // Act
            var result = await _controller.CreateChat(createChatDto);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            createdAtResult.ActionName.Should().Be(nameof(ChatsController.GetChat));
            var returnValue = createdAtResult.Value as ChatDto;
            returnValue.Should().NotBeNull();
            returnValue!.Id.Should().Be("newChat");
        }

        [Fact]
        public async Task AddUserToChat_AsAdmin_ReturnsOkResult()
        {
            // Arrange
            var chatId = "chat123";
            var userId = "newUser";
            var chat = new ChatDto
            {
                Id = chatId,
                Type = "group",
                Participants = new List<ChatParticipantDto>
        {
            new() { UserId = _userId, Role = "admin" }
        }
            };

            _chatServiceMock.Setup(x => x.GetChatByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(chat);
            _chatServiceMock.Setup(x => x.AddUserToChatAsync(It.IsAny<string>(), It.IsAny<string>(), "member"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddUserToChat(chatId, userId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AddUserToChat_NonAdmin_ReturnsForbid()
        {
            // Arrange
            var chatId = "chat123";
            var userId = "newUser";
            var chat = new ChatDto
            {
                Id = chatId,
                Type = "group",
                Participants = new List<ChatParticipantDto>
                {
                    new() { UserId = _userId, Role = "member" }
                }
            };

            _chatServiceMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chat);

            // Act
            var result = await _controller.AddUserToChat(chatId, userId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}