using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ZaloOA.API.Hubs;

[AllowAnonymous]  // Tạm thời cho phép anonymous để test
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group for targeted messages
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific OA conversation group to receive messages
    /// </summary>
    public async Task JoinOAGroup(string oaAccountId)
    {
        var groupName = $"oa_{oaAccountId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined OA group {OAId}", Context.ConnectionId, oaAccountId);
        Console.WriteLine($"[CHATHUB] Client {Context.ConnectionId} joined group: {groupName}");
    }

    /// <summary>
    /// Leave a specific OA conversation group
    /// </summary>
    public async Task LeaveOAGroup(string oaAccountId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"oa_{oaAccountId}");
        _logger.LogInformation("Connection {ConnectionId} left OA group {OAId}", Context.ConnectionId, oaAccountId);
    }
}
