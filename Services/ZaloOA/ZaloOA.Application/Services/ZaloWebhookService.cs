using ZaloOA.Application.Common;
using ZaloOA.Application.DTOs.Webhook;
using ZaloOA.Application.Events;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.Services;

public class ZaloWebhookService : IZaloWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageBroker _messageBroker;
    private readonly IChatNotificationService? _chatNotificationService;

    public ZaloWebhookService(
        IUnitOfWork unitOfWork,
        IMessageBroker messageBroker,
        IChatNotificationService? chatNotificationService = null)
    {
        _unitOfWork = unitOfWork;
        _messageBroker = messageBroker;
        _chatNotificationService = chatNotificationService;
    }

    public bool VerifyWebhook(string? oaId)
    {
        // Basic verification - Zalo sends a GET request to verify the webhook URL
        // In production, you might want to verify against registered OA IDs
        return !string.IsNullOrEmpty(oaId);
    }

    public async Task<Result> ProcessWebhookAsync(ZaloWebhookPayload payload)
    {
        if (string.IsNullOrEmpty(payload.OAId))
        {
            return Result.Failure("OA ID is required");
        }

        // Find OA account by OA ID
        var oaAccounts = await _unitOfWork.ZaloOAAccounts.FindAsync(x => x.OAId == payload.OAId);
        var oaAccount = oaAccounts.FirstOrDefault();

        if (oaAccount == null)
        {
            // OA not registered in our system, skip processing
            return Result.Success();
        }

        return payload.EventName switch
        {
            ZaloWebhookEvents.UserSendText => await HandleUserMessageAsync(payload, oaAccount, MessageType.Text),
            ZaloWebhookEvents.UserSendImage => await HandleUserMessageAsync(payload, oaAccount, MessageType.Image),
            ZaloWebhookEvents.UserSendGif => await HandleUserMessageAsync(payload, oaAccount, MessageType.Gif),
            ZaloWebhookEvents.UserSendSticker => await HandleUserMessageAsync(payload, oaAccount, MessageType.Sticker),
            ZaloWebhookEvents.UserSendFile => await HandleUserMessageAsync(payload, oaAccount, MessageType.File),
            ZaloWebhookEvents.UserSendAudio => await HandleUserMessageAsync(payload, oaAccount, MessageType.Audio),
            ZaloWebhookEvents.UserSendVideo => await HandleUserMessageAsync(payload, oaAccount, MessageType.Video),
            ZaloWebhookEvents.UserSendLocation => await HandleUserMessageAsync(payload, oaAccount, MessageType.Location),
            ZaloWebhookEvents.UserSendBusinessCard => await HandleUserMessageAsync(payload, oaAccount, MessageType.BusinessCard),
            ZaloWebhookEvents.Follow => await HandleFollowEventAsync(payload, oaAccount, true),
            ZaloWebhookEvents.Unfollow => await HandleFollowEventAsync(payload, oaAccount, false),
            _ => Result.Success() // Ignore unknown events
        };
    }

    private async Task<Result> HandleUserMessageAsync(ZaloWebhookPayload payload, ZaloOAAccount oaAccount, MessageType messageType)
    {
        var senderId = payload.Sender?.Id;
        if (string.IsNullOrEmpty(senderId))
        {
            return Result.Failure("Sender ID is required");
        }

        // Get or create ZaloUser
        var zaloUser = await _unitOfWork.ZaloUsers.GetByZaloUserIdAndOAIdAsync(senderId, oaAccount.OAId);
        if (zaloUser == null)
        {
            zaloUser = ZaloUser.Create(senderId, oaAccount.OAId, isFollower: true);
            await _unitOfWork.ZaloUsers.AddAsync(zaloUser);
            await _unitOfWork.SaveChangesAsync();
        }

        // Get or create conversation
        var conversation = await _unitOfWork.ZaloConversations.GetByOAAccountIdAndZaloUserIdAsync(oaAccount.Id, zaloUser.Id);
        if (conversation == null)
        {
            conversation = ZaloConversation.Create(oaAccount.Id, zaloUser.Id);
            await _unitOfWork.ZaloConversations.AddAsync(conversation);
            await _unitOfWork.SaveChangesAsync();
        }

        // Check for duplicate message
        var messageId = payload.Message?.MessageId;
        if (!string.IsNullOrEmpty(messageId))
        {
            var existingMessage = await _unitOfWork.ZaloMessages.GetByZaloMessageIdAsync(messageId);
            if (existingMessage != null)
            {
                return Result.Success(); // Already processed
            }
        }

        // Extract message content and attachment info
        var content = payload.Message?.Text ?? string.Empty;
        string? attachmentUrl = null;
        string? attachmentName = null;
        string? thumbnailUrl = null;

        if (payload.Message?.Attachments != null && payload.Message.Attachments.Any())
        {
            var attachment = payload.Message.Attachments.First();
            attachmentUrl = attachment.Payload?.Url;
            attachmentName = attachment.Payload?.Name;
            thumbnailUrl = attachment.Payload?.Thumbnail;

            if (messageType == MessageType.Location && attachment.Payload?.Coordinates != null)
            {
                content = $"{attachment.Payload.Coordinates.Latitude},{attachment.Payload.Coordinates.Longitude}";
            }
        }

        // Parse timestamp
        var sentAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(payload.Timestamp) && long.TryParse(payload.Timestamp, out var timestamp))
        {
            sentAt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }

        // Create inbound message
        var message = ZaloMessage.CreateInbound(
            conversation.Id,
            messageId,
            messageType,
            content,
            sentAt,
            attachmentUrl,
            attachmentName,
            thumbnailUrl);

        await _unitOfWork.ZaloMessages.AddAsync(message);

        // Update conversation
        var previewContent = string.IsNullOrEmpty(content) ? GetMessageTypePreview(messageType) : content;
        conversation.UpdateLastMessage(previewContent, sentAt);
        conversation.IncrementUnreadCount();
        _unitOfWork.ZaloConversations.Update(conversation);

        // Update user interaction
        zaloUser.UpdateLastInteraction();
        _unitOfWork.ZaloUsers.Update(zaloUser);

        await _unitOfWork.SaveChangesAsync();

        // Send real-time notification via SignalR
        if (_chatNotificationService != null)
        {
            var notification = new NewMessageNotification
            {
                MessageId = message.Id,
                ConversationId = conversation.Id,
                ZaloUserId = senderId,
                SenderName = zaloUser.DisplayName,
                SenderAvatar = zaloUser.AvatarUrl,
                Direction = (int)MessageDirection.Inbound,
                Type = (int)messageType,
                Content = content,
                AttachmentUrl = attachmentUrl,
                ThumbnailUrl = thumbnailUrl,
                SentAt = sentAt
            };
            await _chatNotificationService.NotifyNewMessageAsync(oaAccount.Id, notification);
        }

        // Publish event to message queue for other microservices
        var messageEvent = new UserMessageReceivedEvent
        {
            OAId = oaAccount.OAId,
            OAName = oaAccount.Name,
            SenderId = senderId,
            SenderName = zaloUser.DisplayName,
            MessageId = messageId,
            MessageType = messageType.ToString().ToLower(),
            Content = content,
            AttachmentUrl = attachmentUrl,
            ConversationId = conversation.Id
        };

        await _messageBroker.PublishAsync(
            ZaloEventConstants.ExchangeName,
            ZaloEventConstants.UserMessageReceived,
            messageEvent);

        Console.WriteLine($"[EVENT] Published UserMessageReceivedEvent to RabbitMQ");

        return Result.Success();
    }

    private async Task<Result> HandleFollowEventAsync(ZaloWebhookPayload payload, ZaloOAAccount oaAccount, bool isFollow)
    {
        var followerId = payload.Follower?.Id ?? payload.Sender?.Id;
        if (string.IsNullOrEmpty(followerId))
        {
            return Result.Failure("Follower ID is required");
        }

        var zaloUser = await _unitOfWork.ZaloUsers.GetByZaloUserIdAndOAIdAsync(followerId, oaAccount.OAId);
        if (zaloUser == null)
        {
            if (!isFollow)
            {
                // User unfollowed but was never in our system, ignore
                return Result.Success();
            }

            zaloUser = ZaloUser.Create(followerId, oaAccount.OAId, isFollower: true);
            await _unitOfWork.ZaloUsers.AddAsync(zaloUser);
        }
        else
        {
            zaloUser.SetFollower(isFollow);
            _unitOfWork.ZaloUsers.Update(zaloUser);
        }

        await _unitOfWork.SaveChangesAsync();

        // Publish follow/unfollow event
        var followEvent = new UserFollowEvent
        {
            OAId = oaAccount.OAId,
            OAName = oaAccount.Name,
            UserId = followerId,
            UserName = zaloUser.DisplayName,
            IsFollow = isFollow
        };

        var routingKey = isFollow ? ZaloEventConstants.UserFollow : ZaloEventConstants.UserUnfollow;
        await _messageBroker.PublishAsync(ZaloEventConstants.ExchangeName, routingKey, followEvent);

        Console.WriteLine($"[EVENT] Published UserFollowEvent (isFollow={isFollow}) to RabbitMQ");

        return Result.Success();
    }

    private static string GetMessageTypePreview(MessageType type)
    {
        return type switch
        {
            MessageType.Image => "[Image]",
            MessageType.File => "[File]",
            MessageType.Sticker => "[Sticker]",
            MessageType.Gif => "[GIF]",
            MessageType.Audio => "[Audio]",
            MessageType.Video => "[Video]",
            MessageType.Location => "[Location]",
            MessageType.BusinessCard => "[Business Card]",
            MessageType.List => "[List]",
            _ => "[Message]"
        };
    }
}
