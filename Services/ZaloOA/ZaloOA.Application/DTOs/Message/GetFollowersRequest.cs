namespace ZaloOA.Application.DTOs.Message;

public class GetFollowersRequest
{
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 20;
}
