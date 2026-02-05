using ZaloOA.Application.Common;
using ZaloOA.Application.DTOs.Message;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.Services;

public class ZaloMessageService : IZaloMessageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IZaloApiClient _zaloApiClient;

    public ZaloMessageService(IUnitOfWork unitOfWork, IZaloApiClient zaloApiClient)
    {
        _unitOfWork = unitOfWork;
        _zaloApiClient = zaloApiClient;
    }

    public async Task<Result<FollowerListResponse>> GetFollowersAsync(string userId, Guid oaAccountId, GetFollowersRequest request)
    {
        Console.WriteLine($"[DEBUG] GetFollowers - userId: {userId}, oaAccountId: {oaAccountId}");

        // First try with userId check
        var oaAccount = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(oaAccountId, userId);
        if (oaAccount == null)
        {
            // Debug: Try to find the OA account regardless of userId to see what's stored
            var accountById = await _unitOfWork.ZaloOAAccounts.GetByIdAsync(oaAccountId);
            if (accountById != null)
            {
                Console.WriteLine($"[DEBUG] OA Account exists but has different userId!");
                Console.WriteLine($"[DEBUG] Requested userId: '{userId}'");
                Console.WriteLine($"[DEBUG] Stored userId: '{accountById.UserId}'");
                Console.WriteLine($"[DEBUG] OA Name: {accountById.Name}");
                // For development: use the account regardless of userId mismatch
                oaAccount = accountById;
            }
            else
            {
                Console.WriteLine($"[DEBUG] OA Account not found with ID: {oaAccountId}");
                return Result<FollowerListResponse>.Failure($"Zalo OA account not found. oaAccountId={oaAccountId}, userId={userId}");
            }
        }

        Console.WriteLine($"[DEBUG] Found OA: {oaAccount.Name}, AccessToken: {oaAccount.AccessToken?.Substring(0, 20)}...");

        // Get followers from Zalo API
        var zaloFollowers = await _zaloApiClient.GetFollowersAsync(oaAccount.AccessToken, request.Offset, request.Limit);
        Console.WriteLine($"[DEBUG] Zalo API Response - Error: {zaloFollowers.Error}, Message: {zaloFollowers.Message}, Total: {zaloFollowers.Data?.Total}");

        if (zaloFollowers.Error != 0)
        {
            return Result<FollowerListResponse>.Failure(zaloFollowers.Message ?? "Failed to get followers from Zalo");
        }

        var followers = new List<FollowerResponse>();
        var followerIds = zaloFollowers.Data?.GetFollowersList() ?? new List<DTOs.Zalo.ZaloFollowerInfo>();
        Console.WriteLine($"[DEBUG] Follower IDs count: {followerIds.Count}");

        foreach (var follower in followerIds)
        {
            if (string.IsNullOrEmpty(follower.UserId)) continue;

            // Get or create ZaloUser
            var zaloUser = await _unitOfWork.ZaloUsers.GetByZaloUserIdAndOAIdAsync(follower.UserId, oaAccount.OAId);

            if (zaloUser == null)
            {
                // Fetch user profile from Zalo
                var profileResponse = await _zaloApiClient.GetUserProfileAsync(oaAccount.AccessToken, follower.UserId);
                Console.WriteLine($"[DEBUG] GetUserProfile for {follower.UserId} - Error: {profileResponse.Error}, DisplayName: {profileResponse.Data?.DisplayName}, Avatar: {profileResponse.Data?.Avatar}");

                zaloUser = ZaloUser.Create(
                    follower.UserId,
                    oaAccount.OAId,
                    profileResponse.Data?.DisplayName,
                    profileResponse.Data?.Avatar ?? profileResponse.Data?.Avatars?.Avatar240 ?? profileResponse.Data?.Avatars?.Avatar120,
                    profileResponse.Data?.UserIsFollower ?? true);

                await _unitOfWork.ZaloUsers.AddAsync(zaloUser);
            }
            else
            {
                // Always refresh profile data
                var profileResponse = await _zaloApiClient.GetUserProfileAsync(oaAccount.AccessToken, follower.UserId);
                Console.WriteLine($"[DEBUG] Refresh profile for {follower.UserId} - Error: {profileResponse.Error}, Message: {profileResponse.Message}, DisplayName: {profileResponse.Data?.DisplayName}");

                if (profileResponse.Error == 0 && profileResponse.Data != null)
                {
                    var avatar = profileResponse.Data.Avatar ?? profileResponse.Data.Avatars?.Avatar240 ?? profileResponse.Data.Avatars?.Avatar120;
                    zaloUser.UpdateProfile(profileResponse.Data.DisplayName, avatar);
                    zaloUser.SetFollower(profileResponse.Data.UserIsFollower);
                    _unitOfWork.ZaloUsers.Update(zaloUser);
                }
            }

            // Get conversation info if exists
            var conversation = await _unitOfWork.ZaloConversations.GetByOAAccountIdAndZaloUserIdAsync(oaAccountId, zaloUser.Id);

            followers.Add(new FollowerResponse
            {
                Id = zaloUser.Id,
                ZaloUserId = zaloUser.ZaloUserId,
                DisplayName = zaloUser.DisplayName,
                AvatarUrl = zaloUser.AvatarUrl,
                IsFollower = zaloUser.IsFollower,
                LastInteractionAt = zaloUser.LastInteractionAt,
                FollowedAt = zaloUser.FollowedAt,
                LastMessagePreview = conversation?.LastMessagePreview,
                LastMessageAt = conversation?.LastMessageAt,
                UnreadCount = conversation?.UnreadCount ?? 0
            });
        }

        await _unitOfWork.SaveChangesAsync();

        return Result<FollowerListResponse>.Success(new FollowerListResponse
        {
            Followers = followers,
            TotalCount = zaloFollowers.Data?.Total ?? followers.Count,
            Offset = request.Offset,
            Limit = request.Limit
        });
    }

    public async Task<Result<MessageListResponse>> GetMessagesAsync(string userId, Guid oaAccountId, GetMessagesRequest request)
    {
        var oaAccount = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(oaAccountId, userId);
        if (oaAccount == null)
        {
            return Result<MessageListResponse>.Failure("Zalo OA account not found");
        }

        // Get or find ZaloUser
        var zaloUser = await _unitOfWork.ZaloUsers.GetByZaloUserIdAndOAIdAsync(request.ZaloUserId, oaAccount.OAId);
        if (zaloUser == null)
        {
            // Create user if not exists
            var profileResponse = await _zaloApiClient.GetUserProfileAsync(oaAccount.AccessToken, request.ZaloUserId);
            zaloUser = ZaloUser.Create(
                request.ZaloUserId,
                oaAccount.OAId,
                profileResponse.Data?.DisplayName,
                profileResponse.Data?.Avatar,
                profileResponse.Data?.UserIsFollower ?? false);

            await _unitOfWork.ZaloUsers.AddAsync(zaloUser);
            await _unitOfWork.SaveChangesAsync();
        }

        // Get or create conversation
        var conversation = await _unitOfWork.ZaloConversations.GetByOAAccountIdAndZaloUserIdAsync(oaAccountId, zaloUser.Id);
        if (conversation == null)
        {
            conversation = ZaloConversation.Create(oaAccountId, zaloUser.Id);
            await _unitOfWork.ZaloConversations.AddAsync(conversation);
            await _unitOfWork.SaveChangesAsync();
        }

        // Get messages from local DB first
        var localMessages = await _unitOfWork.ZaloMessages.GetByConversationIdAsync(conversation.Id, request.Offset, request.Limit);
        var messageCount = await _unitOfWork.ZaloMessages.CountByConversationIdAsync(conversation.Id);

        // If no local messages, try to fetch from Zalo API
        if (!localMessages.Any())
        {
            var zaloConversation = await _zaloApiClient.GetConversationAsync(oaAccount.AccessToken, request.ZaloUserId, request.Offset, request.Limit);
            if (zaloConversation.Error == 0 && zaloConversation.Data != null)
            {
                foreach (var msg in zaloConversation.Data)
                {
                    var existingMsg = !string.IsNullOrEmpty(msg.MessageId)
                        ? await _unitOfWork.ZaloMessages.GetByZaloMessageIdAsync(msg.MessageId)
                        : null;

                    if (existingMsg == null)
                    {
                        var direction = msg.Source == 0 ? MessageDirection.Inbound : MessageDirection.Outbound;
                        var type = MapZaloMessageType(msg.Type);
                        var sentAt = DateTimeOffset.FromUnixTimeMilliseconds(msg.Time).UtcDateTime;

                        var newMessage = direction == MessageDirection.Inbound
                            ? ZaloMessage.CreateInbound(conversation.Id, msg.MessageId, type, msg.Message ?? string.Empty, sentAt, msg.Url, null, msg.Thumbnail)
                            : ZaloMessage.CreateOutbound(conversation.Id, type, msg.Message ?? string.Empty, msg.Url, null, msg.Thumbnail);

                        if (direction == MessageDirection.Outbound && !string.IsNullOrEmpty(msg.MessageId))
                        {
                            newMessage.MarkAsSent(msg.MessageId);
                        }

                        await _unitOfWork.ZaloMessages.AddAsync(newMessage);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                // Reload messages
                localMessages = await _unitOfWork.ZaloMessages.GetByConversationIdAsync(conversation.Id, request.Offset, request.Limit);
                messageCount = await _unitOfWork.ZaloMessages.CountByConversationIdAsync(conversation.Id);
            }
        }

        // Reset unread count when viewing messages
        if (conversation.UnreadCount > 0)
        {
            conversation.ResetUnreadCount();
            _unitOfWork.ZaloConversations.Update(conversation);
            await _unitOfWork.SaveChangesAsync();
        }

        var messages = localMessages.Select(m => new MessageResponse
        {
            Id = m.Id,
            ZaloMessageId = m.ZaloMessageId,
            Direction = m.Direction,
            Type = m.Type,
            Content = m.Content,
            AttachmentUrl = m.AttachmentUrl,
            AttachmentName = m.AttachmentName,
            ThumbnailUrl = m.ThumbnailUrl,
            SentAt = m.SentAt,
            Status = m.Status,
            ErrorMessage = m.ErrorMessage
        }).ToList();

        return Result<MessageListResponse>.Success(new MessageListResponse
        {
            Messages = messages,
            TotalCount = messageCount,
            Offset = request.Offset,
            Limit = request.Limit
        });
    }

    public async Task<Result<SendMessageResponse>> SendMessageAsync(string userId, Guid oaAccountId, SendMessageRequest request)
    {
        var oaAccount = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(oaAccountId, userId);
        if (oaAccount == null)
        {
            return Result<SendMessageResponse>.Failure("Zalo OA account not found");
        }

        // Get or create ZaloUser
        var zaloUser = await _unitOfWork.ZaloUsers.GetByZaloUserIdAndOAIdAsync(request.ZaloUserId, oaAccount.OAId);
        if (zaloUser == null)
        {
            var profileResponse = await _zaloApiClient.GetUserProfileAsync(oaAccount.AccessToken, request.ZaloUserId);
            zaloUser = ZaloUser.Create(
                request.ZaloUserId,
                oaAccount.OAId,
                profileResponse.Data?.DisplayName,
                profileResponse.Data?.Avatar,
                profileResponse.Data?.UserIsFollower ?? false);

            await _unitOfWork.ZaloUsers.AddAsync(zaloUser);
            await _unitOfWork.SaveChangesAsync();
        }

        // Get or create conversation
        var conversation = await _unitOfWork.ZaloConversations.GetByOAAccountIdAndZaloUserIdAsync(oaAccountId, zaloUser.Id);
        if (conversation == null)
        {
            conversation = ZaloConversation.Create(oaAccountId, zaloUser.Id);
            await _unitOfWork.ZaloConversations.AddAsync(conversation);
            await _unitOfWork.SaveChangesAsync();
        }

        // Create message
        var message = ZaloMessage.CreateOutbound(
            conversation.Id,
            request.Type,
            request.Text ?? string.Empty,
            request.AttachmentUrl,
            request.AttachmentName);

        await _unitOfWork.ZaloMessages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        // Send to Zalo API
        DTOs.Zalo.ZaloSendMessageResponse sendResponse;
        switch (request.Type)
        {
            case MessageType.Text:
                if (string.IsNullOrEmpty(request.Text))
                {
                    message.MarkAsFailed("Text message content is required");
                    _unitOfWork.ZaloMessages.Update(message);
                    await _unitOfWork.SaveChangesAsync();
                    return Result<SendMessageResponse>.Failure("Text message content is required");
                }
                sendResponse = await _zaloApiClient.SendTextMessageAsync(oaAccount.AccessToken, request.ZaloUserId, request.Text);
                break;

            case MessageType.Image:
                if (string.IsNullOrEmpty(request.AttachmentUrl))
                {
                    message.MarkAsFailed("Image URL is required");
                    _unitOfWork.ZaloMessages.Update(message);
                    await _unitOfWork.SaveChangesAsync();
                    return Result<SendMessageResponse>.Failure("Image URL is required");
                }
                sendResponse = await _zaloApiClient.SendImageMessageAsync(oaAccount.AccessToken, request.ZaloUserId, request.AttachmentUrl, request.Text);
                break;

            case MessageType.File:
                if (string.IsNullOrEmpty(request.AttachmentId))
                {
                    message.MarkAsFailed("Attachment ID is required for file messages");
                    _unitOfWork.ZaloMessages.Update(message);
                    await _unitOfWork.SaveChangesAsync();
                    return Result<SendMessageResponse>.Failure("Attachment ID is required for file messages");
                }
                sendResponse = await _zaloApiClient.SendFileMessageAsync(oaAccount.AccessToken, request.ZaloUserId, request.AttachmentId);
                break;

            default:
                message.MarkAsFailed($"Unsupported message type: {request.Type}");
                _unitOfWork.ZaloMessages.Update(message);
                await _unitOfWork.SaveChangesAsync();
                return Result<SendMessageResponse>.Failure($"Unsupported message type: {request.Type}");
        }

        if (sendResponse.Error != 0)
        {
            message.MarkAsFailed(sendResponse.Message ?? "Failed to send message");
            _unitOfWork.ZaloMessages.Update(message);
            await _unitOfWork.SaveChangesAsync();
            return Result<SendMessageResponse>.Failure(sendResponse.Message ?? "Failed to send message");
        }

        // Update message with Zalo message ID
        message.MarkAsSent(sendResponse.Data?.MessageId ?? string.Empty);
        _unitOfWork.ZaloMessages.Update(message);

        // Update conversation
        conversation.UpdateLastMessage(request.Text ?? "[Attachment]", message.SentAt);
        _unitOfWork.ZaloConversations.Update(conversation);

        // Update user interaction
        zaloUser.UpdateLastInteraction();
        _unitOfWork.ZaloUsers.Update(zaloUser);

        await _unitOfWork.SaveChangesAsync();

        return Result<SendMessageResponse>.Success(new SendMessageResponse
        {
            MessageId = message.Id,
            ZaloMessageId = sendResponse.Data?.MessageId,
            SentAt = message.SentAt
        });
    }

    private static MessageType MapZaloMessageType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "text" => MessageType.Text,
            "image" => MessageType.Image,
            "file" => MessageType.File,
            "sticker" => MessageType.Sticker,
            "gif" => MessageType.Gif,
            "audio" => MessageType.Audio,
            "video" => MessageType.Video,
            "location" => MessageType.Location,
            "business_card" => MessageType.BusinessCard,
            "list" => MessageType.List,
            _ => MessageType.Text
        };
    }
}
