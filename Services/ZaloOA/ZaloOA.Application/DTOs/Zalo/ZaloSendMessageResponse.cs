using System.Text.Json.Serialization;

namespace ZaloOA.Application.DTOs.Zalo;

public class ZaloSendMessageResponse
{
    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public ZaloSendMessageData? Data { get; set; }
}

public class ZaloSendMessageData
{
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
}
