using System.Text.Json.Serialization;

namespace ZaloOA.Application.Events;

/// <summary>
/// Base class for all Zalo events published to message queue
/// </summary>
public abstract class ZaloEventBase
{
    [JsonPropertyName("event_type")]
    public abstract string EventType { get; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("oa_id")]
    public string OAId { get; set; } = string.Empty;

    [JsonPropertyName("oa_name")]
    public string? OAName { get; set; }
}

/// <summary>
/// Event published when a user sends a message to OA
/// </summary>
public class UserMessageReceivedEvent : ZaloEventBase
{
    [JsonPropertyName("event_type")]
    public override string EventType => "user_message_received";

    [JsonPropertyName("sender_id")]
    public string SenderId { get; set; } = string.Empty;

    [JsonPropertyName("sender_name")]
    public string? SenderName { get; set; }

    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = "text";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("attachment_url")]
    public string? AttachmentUrl { get; set; }

    [JsonPropertyName("conversation_id")]
    public Guid ConversationId { get; set; }
}

/// <summary>
/// Event published when a user follows/unfollows OA
/// </summary>
public class UserFollowEvent : ZaloEventBase
{
    [JsonPropertyName("event_type")]
    public override string EventType => IsFollow ? "user_follow" : "user_unfollow";

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("is_follow")]
    public bool IsFollow { get; set; }
}

/// <summary>
/// Event published when OA sends a message
/// </summary>
public class OAMessageSentEvent : ZaloEventBase
{
    [JsonPropertyName("event_type")]
    public override string EventType => "oa_message_sent";

    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; } = string.Empty;

    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }

    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("zalo_message_id")]
    public string? ZaloMessageId { get; set; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = "text";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("conversation_id")]
    public Guid ConversationId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "sent";
}

/// <summary>
/// RabbitMQ exchange and routing key constants
/// </summary>
public static class ZaloEventConstants
{
    public const string ExchangeName = "zalo.events";

    // Routing keys
    public const string UserMessageReceived = "zalo.message.received";
    public const string UserFollow = "zalo.user.follow";
    public const string UserUnfollow = "zalo.user.unfollow";
    public const string OAMessageSent = "zalo.message.sent";
}
