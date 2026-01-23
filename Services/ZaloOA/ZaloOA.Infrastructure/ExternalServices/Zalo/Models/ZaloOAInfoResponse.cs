using System.Text.Json.Serialization;

namespace ZaloOA.Infrastructure.ExternalServices.Zalo.Models;

public class ZaloOAInfoResponse
{
    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public ZaloOAInfoData? Data { get; set; }
}

public class ZaloOAInfoData
{
    [JsonPropertyName("oa_id")]
    public string? OAId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("cover")]
    public string? Cover { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
