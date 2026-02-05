using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.DTOs.Message;

public class MessageListResponse
{
    public List<MessageResponse> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
}

public class MessageResponse
{
    public Guid Id { get; set; }
    public string? ZaloMessageId { get; set; }
    public MessageDirection Direction { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = null!;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime SentAt { get; set; }
    public MessageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
