using Chat.API.Authentication;
using Chat.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WebPubSubController : ControllerBase
{
    private readonly IWebPubSubService _webPubSubService;
    private string UserId => User.GetUserId();

    public WebPubSubController(IWebPubSubService webPubSubService)
    {
        _webPubSubService = webPubSubService;
    }

    /// <summary>
    /// Gets a connection token for WebSocket connection
    /// </summary>
    /// <returns>WebSocket connection URL with access token</returns>
    [HttpGet("token")]
    public async Task<IActionResult> GetConnectionToken()
    {
        var url = await _webPubSubService.GetClientConnectionUrlAsync(UserId);
        return Ok(new { url });
    }
}