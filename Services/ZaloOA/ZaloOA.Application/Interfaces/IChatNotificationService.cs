namespace ZaloOA.Application.Interfaces;

public interface IChatNotificationService
{
    Task NotifyNewMessageAsync(Guid oaAccountId, NewMessageNotification notification);
    Task NotifyMessageStatusAsync(Guid oaAccountId, MessageStatusNotification notification);
}

public class NewMessageNotification
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public string ZaloUserId { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public string? SenderAvatar { get; set; }
    public int Direction { get; set; } // 0 = Inbound, 1 = Outbound
    public int Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime SentAt { get; set; }
}

public class MessageStatusNotification
{
    public Guid MessageId { get; set; }
    public int Status { get; set; }
    public string? ErrorMessage { get; set; }
}
