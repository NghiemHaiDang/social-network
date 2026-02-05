using ZaloOA.Application.Common;
using ZaloOA.Application.DTOs.Message;

namespace ZaloOA.Application.Interfaces;

public interface IZaloMessageService
{
    Task<Result<FollowerListResponse>> GetFollowersAsync(string userId, Guid oaAccountId, GetFollowersRequest request);
    Task<Result<MessageListResponse>> GetMessagesAsync(string userId, Guid oaAccountId, GetMessagesRequest request);
    Task<Result<SendMessageResponse>> SendMessageAsync(string userId, Guid oaAccountId, SendMessageRequest request);
}
