using ZaloOA.Domain.Common;
using ZaloOA.Domain.Enums;
using ZaloOA.Domain.Exceptions;

namespace ZaloOA.Domain.Entities;

public class ZaloConversation : BaseEntity
{
    public Guid OAAccountId { get; private set; }
    public Guid ZaloUserId { get; private set; }
    public string? LastMessagePreview { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public int UnreadCount { get; private set; }
    public ConversationStatus Status { get; private set; }

    // Navigation properties
    public ZaloOAAccount OAAccount { get; private set; } = null!;
    public ZaloUser ZaloUser { get; private set; } = null!;
    public ICollection<ZaloMessage> Messages { get; private set; } = new List<ZaloMessage>();

    private ZaloConversation() { }

    public static ZaloConversation Create(Guid oaAccountId, Guid zaloUserId)
    {
        if (oaAccountId == Guid.Empty)
            throw new DomainException("OA Account ID cannot be empty.");

        if (zaloUserId == Guid.Empty)
            throw new DomainException("Zalo User ID cannot be empty.");

        return new ZaloConversation
        {
            OAAccountId = oaAccountId,
            ZaloUserId = zaloUserId,
            UnreadCount = 0,
            Status = ConversationStatus.Active
        };
    }

    public void UpdateLastMessage(string? preview, DateTime messageTime)
    {
        LastMessagePreview = preview?.Length > 100 ? preview.Substring(0, 100) + "..." : preview;
        LastMessageAt = messageTime;
        SetUpdatedAt();
    }

    public void IncrementUnreadCount()
    {
        UnreadCount++;
        SetUpdatedAt();
    }

    public void ResetUnreadCount()
    {
        UnreadCount = 0;
        SetUpdatedAt();
    }

    public void Archive()
    {
        Status = ConversationStatus.Archived;
        SetUpdatedAt();
    }

    public void Activate()
    {
        Status = ConversationStatus.Active;
        SetUpdatedAt();
    }
}
