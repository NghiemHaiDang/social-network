using ZaloOA.Domain.Common;
using ZaloOA.Domain.Enums;
using ZaloOA.Domain.Exceptions;

namespace ZaloOA.Domain.Entities;

public class ZaloMessage : BaseEntity
{
    public Guid ConversationId { get; private set; }
    public string? ZaloMessageId { get; private set; }
    public MessageDirection Direction { get; private set; }
    public MessageType Type { get; private set; }
    public string Content { get; private set; } = null!;
    public string? AttachmentUrl { get; private set; }
    public string? AttachmentName { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public DateTime SentAt { get; private set; }
    public MessageStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Navigation property
    public ZaloConversation Conversation { get; private set; } = null!;

    private ZaloMessage() { }

    public static ZaloMessage CreateOutbound(
        Guid conversationId,
        MessageType type,
        string content,
        string? attachmentUrl = null,
        string? attachmentName = null,
        string? thumbnailUrl = null)
    {
        if (conversationId == Guid.Empty)
            throw new DomainException("Conversation ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(attachmentUrl))
            throw new DomainException("Message content or attachment is required.");

        return new ZaloMessage
        {
            ConversationId = conversationId,
            Direction = MessageDirection.Outbound,
            Type = type,
            Content = content ?? string.Empty,
            AttachmentUrl = attachmentUrl,
            AttachmentName = attachmentName,
            ThumbnailUrl = thumbnailUrl,
            SentAt = DateTime.UtcNow,
            Status = MessageStatus.Pending
        };
    }

    public static ZaloMessage CreateInbound(
        Guid conversationId,
        string? zaloMessageId,
        MessageType type,
        string content,
        DateTime sentAt,
        string? attachmentUrl = null,
        string? attachmentName = null,
        string? thumbnailUrl = null)
    {
        if (conversationId == Guid.Empty)
            throw new DomainException("Conversation ID cannot be empty.");

        return new ZaloMessage
        {
            ConversationId = conversationId,
            ZaloMessageId = zaloMessageId,
            Direction = MessageDirection.Inbound,
            Type = type,
            Content = content ?? string.Empty,
            AttachmentUrl = attachmentUrl,
            AttachmentName = attachmentName,
            ThumbnailUrl = thumbnailUrl,
            SentAt = sentAt,
            Status = MessageStatus.Delivered
        };
    }

    public void MarkAsSent(string zaloMessageId)
    {
        ZaloMessageId = zaloMessageId;
        Status = MessageStatus.Sent;
        SetUpdatedAt();
    }

    public void MarkAsDelivered()
    {
        Status = MessageStatus.Delivered;
        SetUpdatedAt();
    }

    public void MarkAsRead()
    {
        Status = MessageStatus.Read;
        SetUpdatedAt();
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = MessageStatus.Failed;
        ErrorMessage = errorMessage;
        SetUpdatedAt();
    }
}
