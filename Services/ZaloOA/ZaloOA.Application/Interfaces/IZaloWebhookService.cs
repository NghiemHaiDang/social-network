using ZaloOA.Application.Common;
using ZaloOA.Application.DTOs.Webhook;

namespace ZaloOA.Application.Interfaces;

public interface IZaloWebhookService
{
    Task<Result> ProcessWebhookAsync(ZaloWebhookPayload payload);
    bool VerifyWebhook(string? oaId);
}
