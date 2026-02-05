using System.Text.Json.Serialization;

namespace ZaloOA.Application.DTOs.Zalo;

public class ZaloTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("error")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
