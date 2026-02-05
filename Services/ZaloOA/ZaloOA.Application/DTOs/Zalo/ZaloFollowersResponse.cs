using System.Text.Json.Serialization;

namespace ZaloOA.Application.DTOs.Zalo;

public class ZaloFollowersResponse
{
    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public ZaloFollowersData? Data { get; set; }
}

public class ZaloFollowersData
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    // API v2 uses "followers", API v3 uses "users"
    [JsonPropertyName("followers")]
    public List<ZaloFollowerInfo>? Followers { get; set; }

    [JsonPropertyName("users")]
    public List<ZaloFollowerInfo>? Users { get; set; }

    // Helper to get the list regardless of API version
    public List<ZaloFollowerInfo> GetFollowersList() => Users ?? Followers ?? new List<ZaloFollowerInfo>();
}

public class ZaloFollowerInfo
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
