using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZaloOA.Application.DTOs.ZaloOA;
using ZaloOA.Application.Interfaces;

namespace ZaloOA.API.Controllers;

[ApiController]
[Route("api/zalooa")]
[Authorize]
public class ZaloOAController : ControllerBase
{
    private readonly IZaloOAService _zaloOAService;

    public ZaloOAController(IZaloOAService zaloOAService)
    {
        _zaloOAService = zaloOAService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpGet("oauth/authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOAuthAuthorizeUrl([FromQuery] string? redirectUri = null)
    {
        var result = await _zaloOAService.GetOAuth2AuthorizeUrlAsync(redirectUri);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("oauth/connect")]
    public async Task<IActionResult> ConnectWithOAuth2([FromBody] ConnectOAuth2Request request)
    {
        var userId = GetUserId();
        var result = await _zaloOAService.ConnectWithOAuth2Async(userId, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("connect/apikey")]
    public async Task<IActionResult> ConnectWithApiKey([FromBody] ConnectApiKeyRequest request)
    {
        var userId = GetUserId();
        var result = await _zaloOAService.ConnectWithApiKeyAsync(userId, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetConnectedAccounts()
    {
        var userId = GetUserId();
        var result = await _zaloOAService.GetConnectedAccountsAsync(userId);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var userId = GetUserId();
        var result = await _zaloOAService.GetAccountByIdAsync(userId, id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DisconnectAccount(Guid id)
    {
        var userId = GetUserId();
        var result = await _zaloOAService.DisconnectAccountAsync(userId, id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return NoContent();
    }

    [HttpPost("{id:guid}/refresh-token")]
    public async Task<IActionResult> RefreshToken(Guid id)
    {
        var userId = GetUserId();
        var result = await _zaloOAService.RefreshTokenAsync(userId, id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }
}
