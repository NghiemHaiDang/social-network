using System.Text.Json.Serialization;

namespace ZaloOA.Application.DTOs.Webhook;

public class ZaloWebhookPayload
{
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    [JsonPropertyName("oa_id")]
    public string? OAId { get; set; }

    [JsonPropertyName("user_id_by_app")]
    public string? UserIdByApp { get; set; }

    [JsonPropertyName("event_name")]
    public string? EventName { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("sender")]
    public ZaloWebhookSender? Sender { get; set; }

    [JsonPropertyName("recipient")]
    public ZaloWebhookRecipient? Recipient { get; set; }

    [JsonPropertyName("message")]
    public ZaloWebhookMessage? Message { get; set; }

    [JsonPropertyName("follower")]
    public ZaloWebhookFollower? Follower { get; set; }
}

public class ZaloWebhookSender
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class ZaloWebhookRecipient
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class ZaloWebhookMessage
{
    [JsonPropertyName("msg_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public List<ZaloWebhookAttachment>? Attachments { get; set; }
}

public class ZaloWebhookAttachment
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("payload")]
    public ZaloWebhookAttachmentPayload? Payload { get; set; }
}

public class ZaloWebhookAttachmentPayload
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("coordinates")]
    public ZaloWebhookCoordinates? Coordinates { get; set; }
}

public class ZaloWebhookCoordinates
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

public class ZaloWebhookFollower
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

// Webhook event names
public static class ZaloWebhookEvents
{
    public const string UserSendText = "user_send_text";
    public const string UserSendImage = "user_send_image";
    public const string UserSendGif = "user_send_gif";
    public const string UserSendSticker = "user_send_sticker";
    public const string UserSendFile = "user_send_file";
    public const string UserSendAudio = "user_send_audio";
    public const string UserSendVideo = "user_send_video";
    public const string UserSendLocation = "user_send_location";
    public const string UserSendBusinessCard = "user_send_business_card";
    public const string Follow = "follow";
    public const string Unfollow = "unfollow";
    public const string OASendText = "oa_send_text";
    public const string OASendImage = "oa_send_image";
    public const string OASendFile = "oa_send_file";
    public const string OASendGif = "oa_send_gif";
    public const string OASendList = "oa_send_list";
}
