using Microsoft.AspNetCore.SignalR;
using ZaloOA.API.Hubs;
using ZaloOA.Application.Interfaces;

namespace ZaloOA.API.Services;

public class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatNotificationService> _logger;

    public ChatNotificationService(
        IHubContext<ChatHub> hubContext,
        ILogger<ChatNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewMessageAsync(Guid oaAccountId, NewMessageNotification notification)
    {
        var groupName = $"oa_{oaAccountId}";
        _logger.LogInformation("Sending new message notification to group {Group}", groupName);

        Console.WriteLine($"[SIGNALR] ========================================");
        Console.WriteLine($"[SIGNALR] Sending NewMessage to group: {groupName}");
        Console.WriteLine($"[SIGNALR] Content: {notification.Content}");
        Console.WriteLine($"[SIGNALR] ZaloUserId: {notification.ZaloUserId}");
        Console.WriteLine($"[SIGNALR] ========================================");

        await _hubContext.Clients.Group(groupName).SendAsync("NewMessage", notification);

        Console.WriteLine($"[SIGNALR] Sent successfully!");
    }

    public async Task NotifyMessageStatusAsync(Guid oaAccountId, MessageStatusNotification notification)
    {
        var groupName = $"oa_{oaAccountId}";
        await _hubContext.Clients.Group(groupName).SendAsync("MessageStatus", notification);
    }
}
