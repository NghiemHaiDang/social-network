using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZaloOA.Application.DTOs.ZaloOA;
using ZaloOA.Application.Interfaces;
using ZaloOA.Application.Services;

namespace ZaloOA.API.Controllers;

[ApiController]
[Route("api/zalooa")]
// [Authorize]
public class ZaloOAController : ControllerBase
{
    private readonly IZaloOAService _zaloOAService;
    private readonly ZaloSettings _zaloSettings;

    public ZaloOAController(IZaloOAService zaloOAService, ZaloSettings zaloSettings)
    {
        _zaloOAService = zaloOAService;
        _zaloSettings = zaloSettings;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Request.Headers["X-User-Id"].FirstOrDefault()
        ?? "default-user"; // Fallback cho development

    /// <summary>
    /// Redirect to Zalo OAuth page for authorization
    /// </summary>
    [HttpGet("oauth/authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOAuthAuthorizeUrl([FromQuery] string? redirectUri = null, [FromQuery] string? userId = null)
    {
        var effectiveUserId = userId ?? (User.Identity?.IsAuthenticated == true ? GetUserId() : null);

        if (string.IsNullOrEmpty(effectiveUserId))
            return BadRequest(new { error = "User ID is required. Pass userId query parameter or use JWT authentication." });

        var result = await _zaloOAService.GetOAuth2AuthorizeUrlAsync(effectiveUserId, redirectUri);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        // Redirect directly to Zalo OAuth page
        return Redirect(result.Data!.AuthorizeUrl);
    }

    /// <summary>
    /// OAuth callback endpoint - Zalo redirects here after user authorization
    /// </summary>
    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? oa_id)
    {
        Console.WriteLine($"[DEBUG] ========== OAuth Callback Received ==========");
        Console.WriteLine($"[DEBUG] Code: {code?.Substring(0, Math.Min(30, code?.Length ?? 0))}...");
        Console.WriteLine($"[DEBUG] State: {state?.Substring(0, Math.Min(30, state?.Length ?? 0))}...");
        Console.WriteLine($"[DEBUG] Error: {error}");
        Console.WriteLine($"[DEBUG] OA_ID: {oa_id}");
        Console.WriteLine($"[DEBUG] =================================================");

        var frontendCallback = $"{_zaloSettings.FrontendBaseUrl}/zalo/callback";

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"[DEBUG] OAuth Error from Zalo: {error}");
            return Redirect($"{frontendCallback}?success=false&error={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            Console.WriteLine($"[DEBUG] Missing parameters - code: {code != null}, state: {state != null}");
            return Redirect($"{frontendCallback}?success=false&error=missing_parameters");
        }

        var result = await _zaloOAService.HandleOAuthCallbackAsync(code, state);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"[DEBUG] HandleOAuthCallback failed: {result.Error}");
            return Redirect($"{frontendCallback}?success=false&error={Uri.EscapeDataString(result.Error ?? "unknown_error")}");
        }

        Console.WriteLine($"[DEBUG] OAuth Success! Redirecting to: {result.Data!.RedirectUrl}");
        return Redirect(result.Data!.RedirectUrl);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok(new {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            config = new {
                appId = _zaloSettings.AppId,
                redirectUri = _zaloSettings.DefaultRedirectUri,
                frontendUrl = _zaloSettings.FrontendBaseUrl
            }
        });
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
