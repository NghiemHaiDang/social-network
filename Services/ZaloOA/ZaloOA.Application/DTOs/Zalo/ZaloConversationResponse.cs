using System.Text.Json.Serialization;

namespace ZaloOA.Application.DTOs.Zalo;

public class ZaloConversationResponse
{
    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public List<ZaloConversationMessage>? Data { get; set; }
}

public class ZaloConversationMessage
{
    [JsonPropertyName("msg_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("src")]
    public int Source { get; set; } // 0 = user, 1 = OA

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("from_id")]
    public string? FromId { get; set; }

    [JsonPropertyName("to_id")]
    public string? ToId { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumb")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
