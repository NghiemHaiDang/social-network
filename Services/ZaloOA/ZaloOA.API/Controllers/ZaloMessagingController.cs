using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZaloOA.Application.DTOs.Message;
using ZaloOA.Application.Interfaces;

namespace ZaloOA.API.Controllers;

[ApiController]
[Route("api/zalooa/{oaAccountId:guid}")]
[Authorize]
public class ZaloMessagingController : ControllerBase
{
    private readonly IZaloMessageService _messageService;

    public ZaloMessagingController(IZaloMessageService messageService)
    {
        _messageService = messageService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get list of followers/users who have interacted with the OA
    /// </summary>
    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers(
        Guid oaAccountId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        var request = new GetFollowersRequest { Offset = offset, Limit = limit };
        var result = await _messageService.GetFollowersAsync(userId, oaAccountId, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get message history with a specific Zalo user
    /// </summary>
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(
        Guid oaAccountId,
        [FromQuery] string zaloUserId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50)
    {
        if (string.IsNullOrEmpty(zaloUserId))
            return BadRequest(new { error = "zaloUserId is required" });

        var userId = GetUserId();
        var request = new GetMessagesRequest
        {
            ZaloUserId = zaloUserId,
            Offset = offset,
            Limit = limit
        };

        var result = await _messageService.GetMessagesAsync(userId, oaAccountId, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Send a message to a Zalo user
    /// </summary>
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(
        Guid oaAccountId,
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrEmpty(request.ZaloUserId))
            return BadRequest(new { error = "zaloUserId is required" });

        var userId = GetUserId();
        var result = await _messageService.SendMessageAsync(userId, oaAccountId, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }
}
