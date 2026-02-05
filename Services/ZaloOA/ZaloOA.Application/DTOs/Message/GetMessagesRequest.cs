namespace ZaloOA.Application.DTOs.Message;

public class GetMessagesRequest
{
    public string ZaloUserId { get; set; } = null!;
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 50;
}
