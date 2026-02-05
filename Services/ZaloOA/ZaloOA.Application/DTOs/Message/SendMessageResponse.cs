namespace ZaloOA.Application.DTOs.Message;

public class SendMessageResponse
{
    public Guid MessageId { get; set; }
    public string? ZaloMessageId { get; set; }
    public DateTime SentAt { get; set; }
}
