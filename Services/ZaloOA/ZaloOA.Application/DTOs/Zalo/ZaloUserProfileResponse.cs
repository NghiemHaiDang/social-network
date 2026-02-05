using System.Text.Json.Serialization;

namespace ZaloOA.Application.DTOs.Zalo;

public class ZaloUserProfileResponse
{
    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public ZaloUserProfileData? Data { get; set; }
}

public class ZaloUserProfileData
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("user_alias")]
    public string? UserAlias { get; set; }

    [JsonPropertyName("user_is_follower")]
    public bool UserIsFollower { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("avatars")]
    public ZaloAvatars? Avatars { get; set; }
}

public class ZaloAvatars
{
    [JsonPropertyName("120")]
    public string? Avatar120 { get; set; }

    [JsonPropertyName("240")]
    public string? Avatar240 { get; set; }
}
