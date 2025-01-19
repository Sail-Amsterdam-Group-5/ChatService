using Chat.API.Controllers;
using Chat.Core.Interfaces;
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
    public class WebPubSubControllerTests
    {
        private readonly Mock<IWebPubSubService> _webPubSubServiceMock;
        private readonly WebPubSubController _controller;
        private readonly string _userId = "testUser";

        public WebPubSubControllerTests()
        {
            _webPubSubServiceMock = new Mock<IWebPubSubService>();
            _controller = new WebPubSubController(_webPubSubServiceMock.Object);

            // Setup controller with mock user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.NameIdentifier, _userId),
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetConnectionToken_ReturnsOkResult()
        {
            // Arrange
            var connectionUrl = "wss://test.webpubsub.azure.com/client/token";
            _webPubSubServiceMock.Setup(x => x.GetClientConnectionUrlAsync(_userId))
                .ReturnsAsync(connectionUrl);

            // Act
            var result = await _controller.GetConnectionToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.Value.Should().NotBeNull();
            var resultValue = okResult.Value.GetType().GetProperty("url")?.GetValue(okResult.Value);
            Assert.Equal(connectionUrl, resultValue);
        }
    }
}
