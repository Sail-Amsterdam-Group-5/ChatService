using Chat.API.Controllers;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Chat.API.Tests.Controllers
{
    public class MessagesControllerTests
    {
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly Mock<IChatService> _chatServiceMock;
        private readonly Mock<IDeletedMessageService> _deletedMessageServiceMock;
        private readonly MessagesController _controller;
        private readonly string _userId = "testUser";

        public MessagesControllerTests()
        {
            _messageServiceMock = new Mock<IMessageService>();
            _chatServiceMock = new Mock<IChatService>();
            _deletedMessageServiceMock = new Mock<IDeletedMessageService>();

            _controller = new MessagesController(
                _messageServiceMock.Object,
                _chatServiceMock.Object,
                _deletedMessageServiceMock.Object);

            // Setup controller with mock user
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
        public async Task GetChatMessages_ValidChat_ReturnsOkResult()
        {
            // Arrange
            var chatId = "chat123";
            var chat = new ChatDto
            {
                Id = chatId,
                Participants = new List<ChatParticipantDto>
            {
                new() { UserId = _userId, Role = "member" }
            }
            };

            var messages = new List<MessageDto>
        {
            new() { Id = "msg1", ChatId = chatId },
            new() { Id = "msg2", ChatId = chatId }
        };

            _chatServiceMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chat);
            _messageServiceMock.Setup(x => x.GetChatMessagesAsync(chatId, 1, 50))
                .ReturnsAsync(messages);

            // Act
            var result = await _controller.GetChatMessages(chatId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedMessages = Assert.IsAssignableFrom<IEnumerable<MessageDto>>(okResult.Value);
            returnedMessages.Should().HaveCount(2);
        }

        [Fact]
        public async Task SendMessage_ValidTextMessage_ReturnsCreatedResult()
        {
            // Arrange
            var chatId = "chat123";
            var createMessageDto = new CreateMessageDto
            {
                ChatId = chatId,
                Type = "text",
                Content = new MessageContentDto { Text = "Hello" }
            };

            var chat = new ChatDto
            {
                Id = chatId,
                Participants = new List<ChatParticipantDto>
            {
                new() { UserId = _userId, Role = "member" }
            }
            };

            var createdMessage = new MessageDto
            {
                Id = "msg1",
                ChatId = chatId,
                Type = "text",
                Content = new MessageContentDto { Text = "Hello" }
            };

            _chatServiceMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chat);
            _messageServiceMock.Setup(x => x.SendMessageAsync(createMessageDto, _userId))
                .ReturnsAsync(createdMessage);

            // Act
            var result = await _controller.SendMessage(createMessageDto);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnedMessage = Assert.IsType<MessageDto>(createdAtResult.Value);
            returnedMessage.Id.Should().Be("msg1");
        }

        [Fact]
        public async Task DeleteMessage_AsOwner_ReturnsOkResult()
        {
            // Arrange
            var messageId = "msg1";
            var chatId = "chat123";

            var chat = new ChatDto
            {
                Id = chatId,
                Participants = new List<ChatParticipantDto>
                {
                    new() { UserId = _userId, Role = "member" }
                }
            };

            var message = new MessageDto
            {
                Id = messageId,
                ChatId = chatId,
                SenderId = _userId
            };

            // Mock the chat service first
            _chatServiceMock.Setup(x => x.GetChatByIdAsync(chatId))
                .ReturnsAsync(chat);

            // Then mock the message service
            _messageServiceMock.Setup(x => x.GetMessageByIdAsync(messageId, chatId))
                .ReturnsAsync(message);
            _messageServiceMock.Setup(x => x.DeleteMessageAsync(messageId, chatId, _userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteMessage(messageId, chatId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task GetDeletedMessages_ReturnsOkResult()
        {
            // Arrange
            var lastSyncTime = DateTime.UtcNow.AddHours(-1);
            var deletedMessages = new List<DeletedMessageDto>
    {
        new() { MessageId = "msg1", ChatId = "chat1" },
        new() { MessageId = "msg2", ChatId = "chat2" }
    };

            _deletedMessageServiceMock.Setup(x => x.GetDeletedMessagesAfterAsync(lastSyncTime))
                .ReturnsAsync(deletedMessages);

            // Act
            var result = await _controller.GetDeletedMessages(lastSyncTime);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = okResult.Value as object;
            Assert.NotNull(returnValue);

            // Use reflection to check the property
            var property = returnValue.GetType().GetProperty("deletedMessages");
            Assert.NotNull(property);

            var messages = property.GetValue(returnValue) as IEnumerable<DeletedMessageDto>;
            Assert.NotNull(messages);
            Assert.Equal(2, messages.Count());
        }
    }
}
