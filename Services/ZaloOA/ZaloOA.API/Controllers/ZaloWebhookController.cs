using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZaloOA.Application.DTOs.Webhook;
using ZaloOA.Application.Interfaces;

namespace ZaloOA.API.Controllers;

[ApiController]
[Route("api/zalooa/webhook")]
[AllowAnonymous] 
public class ZaloWebhookController : ControllerBase
{
    private readonly IZaloWebhookService _webhookService;
    private readonly ILogger<ZaloWebhookController> _logger;

    public ZaloWebhookController(
        IZaloWebhookService webhookService,
        ILogger<ZaloWebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Webhook verification endpoint - called by Zalo to verify the webhook URL
    /// Must always return 200 OK for Zalo to accept the webhook
    /// </summary>
    [HttpGet]
    public IActionResult VerifyWebhook([FromQuery] string? oa_id)
    {
        _logger.LogInformation("Webhook verification request received for OA: {OAId}", oa_id);
        Console.WriteLine($"[WEBHOOK] Verification request - OA_ID: {oa_id}");

        // Always return 200 OK for webhook verification
        return Ok(new { status = "ok", message = "Webhook verified successfully" });
    }

    /// <summary>
    /// Receive webhook events from Zalo
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] ZaloWebhookPayload payload)
    {
        Console.WriteLine($"[WEBHOOK] ========== Webhook Received ==========");
        Console.WriteLine($"[WEBHOOK] Event: {payload.EventName}");
        Console.WriteLine($"[WEBHOOK] OA ID (original): {payload.OAId}");
        Console.WriteLine($"[WEBHOOK] Sender ID: {payload.Sender?.Id}");
        Console.WriteLine($"[WEBHOOK] Recipient ID: {payload.Recipient?.Id}");
        Console.WriteLine($"[WEBHOOK] Message: {payload.Message?.Text}");
        Console.WriteLine($"[WEBHOOK] Timestamp: {payload.Timestamp}");

        // Zalo có thể gửi OA ID trong recipient.id thay vì oa_id
        // Với event user_send_*, sender là user, recipient là OA
        if (string.IsNullOrEmpty(payload.OAId))
        {
            if (payload.EventName?.StartsWith("user_send") == true && !string.IsNullOrEmpty(payload.Recipient?.Id))
            {
                payload.OAId = payload.Recipient.Id;
                Console.WriteLine($"[WEBHOOK] OA ID (from recipient): {payload.OAId}");
            }
            else if (payload.EventName?.StartsWith("oa_send") == true && !string.IsNullOrEmpty(payload.Sender?.Id))
            {
                payload.OAId = payload.Sender.Id;
                Console.WriteLine($"[WEBHOOK] OA ID (from sender): {payload.OAId}");
            }
        }

        Console.WriteLine($"[WEBHOOK] ==========================================");

        _logger.LogInformation(
            "Webhook received - Event: {Event}, OA: {OAId}, Sender: {SenderId}",
            payload.EventName,
            payload.OAId,
            payload.Sender?.Id);

        try
        {
            var result = await _webhookService.ProcessWebhookAsync(payload);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"[WEBHOOK] Processing failed: {result.Error}");
                _logger.LogWarning("Failed to process webhook: {Error}", result.Error);
            }
            else
            {
                Console.WriteLine($"[WEBHOOK] Processing success!");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WEBHOOK] Exception: {ex.Message}");
            _logger.LogError(ex, "Error processing webhook");
            return Ok();
        }
    }
}
