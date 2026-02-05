using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZaloOA.Application.DTOs.Message;
using ZaloOA.Application.Interfaces;

namespace ZaloOA.API.Controllers;

[ApiController]
[Route("api/zalooa/{oaAccountId:guid}")]
// [Authorize] // Temporarily disabled for debugging
public class ZaloMessagingController : ControllerBase
{
    private readonly IZaloMessageService _messageService;

    public ZaloMessagingController(IZaloMessageService messageService)
    {
        _messageService = messageService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Request.Headers["X-User-Id"].FirstOrDefault()
        ?? "default-user"; // Fallback cho development

    /// <summary>
    /// Get list of followers/users who have interacted with the OA
    /// </summary>
    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers(
        Guid oaAccountId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20)
    {
        try
        {
            // Debug: Log all claims
            Console.WriteLine($"[DEBUG] ========== GetFollowers Request ==========");
            Console.WriteLine($"[DEBUG] OA Account ID: {oaAccountId}");
            Console.WriteLine($"[DEBUG] User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"[DEBUG] User.Identity.Name: {User.Identity?.Name}");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"[DEBUG] Claim: {claim.Type} = {claim.Value}");
            }

            var userId = GetUserId();
            Console.WriteLine($"[DEBUG] Resolved UserId: {userId}");
            Console.WriteLine($"[DEBUG] =============================================");

            var request = new GetFollowersRequest { Offset = offset, Limit = limit };
            var result = await _messageService.GetFollowersAsync(userId, oaAccountId, request);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error, debug = new { userId, oaAccountId } });

            // Debug: return raw data
            return Ok(new {
                followers = result.Data?.Followers,
                totalCount = result.Data?.TotalCount,
                offset = result.Data?.Offset,
                limit = result.Data?.Limit,
                followersCount = result.Data?.Followers?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in GetFollowers: {ex}");
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
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
    /// Test: Get raw user profile from Zalo API
    /// </summary>
    [HttpGet("test/profile/{zaloUserId}")]
    [AllowAnonymous]
    public async Task<IActionResult> TestGetProfile(Guid oaAccountId, string zaloUserId)
    {
        try
        {
            var userId = GetUserId();
            var oaAccount = await GetOAAccountAsync(oaAccountId, userId);
            if (oaAccount == null)
                return BadRequest(new { error = "OA account not found" });

            using var httpClient = new HttpClient();

            // Try POST with JSON body (API v3 style)
            var payload = new { user_id = zaloUserId };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://openapi.zalo.me/v3.0/oa/user/detail")
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("access_token", oaAccount.AccessToken);

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Ok(new {
                method = "POST",
                rawResponse = content,
                statusCode = response.StatusCode
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test: Get conversation from Zalo API
    /// </summary>
    [HttpGet("test/conversation/{zaloUserId}")]
    [AllowAnonymous]
    public async Task<IActionResult> TestGetConversation(Guid oaAccountId, string zaloUserId)
    {
        try
        {
            var userId = GetUserId();
            var oaAccount = await GetOAAccountAsync(oaAccountId, userId);
            if (oaAccount == null)
                return BadRequest(new { error = "OA account not found" });

            using var httpClient = new HttpClient();

            // Try both GET and POST
            // GET method
            var getRequest = new HttpRequestMessage(HttpMethod.Get,
                $"https://openapi.zalo.me/v3.0/oa/conversation?user_id={zaloUserId}&offset=0&count=10");
            getRequest.Headers.Add("access_token", oaAccount.AccessToken);
            var getResponse = await httpClient.SendAsync(getRequest);
            var getContent = await getResponse.Content.ReadAsStringAsync();

            // POST method
            var payload = new { user_id = zaloUserId, offset = 0, count = 10 };
            var postRequest = new HttpRequestMessage(HttpMethod.Post, "https://openapi.zalo.me/v3.0/oa/conversation")
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            postRequest.Headers.Add("access_token", oaAccount.AccessToken);
            var postResponse = await httpClient.SendAsync(postRequest);
            var postContent = await postResponse.Content.ReadAsStringAsync();

            return Ok(new {
                getMethod = new { response = getContent, status = getResponse.StatusCode },
                postMethod = new { response = postContent, status = postResponse.StatusCode }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<ZaloOA.Domain.Entities.ZaloOAAccount?> GetOAAccountAsync(Guid oaAccountId, string userId)
    {
        // Simple query - in real app, inject repository
        var dbContext = HttpContext.RequestServices.GetRequiredService<ZaloOA.Infrastructure.Data.ZaloOADbContext>();
        return await dbContext.ZaloOAAccounts.FindAsync(oaAccountId);
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
