using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.DTOs.Message;

public class SendMessageRequest
{
    public string ZaloUserId { get; set; } = null!;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? Text { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentId { get; set; }
}
