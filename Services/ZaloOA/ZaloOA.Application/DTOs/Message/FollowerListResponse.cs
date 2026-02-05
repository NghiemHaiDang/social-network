namespace ZaloOA.Application.DTOs.Message;

public class FollowerListResponse
{
    public List<FollowerResponse> Followers { get; set; } = new();
    public int TotalCount { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
}

public class FollowerResponse
{
    public Guid Id { get; set; }
    public string ZaloUserId { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsFollower { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public DateTime? FollowedAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
