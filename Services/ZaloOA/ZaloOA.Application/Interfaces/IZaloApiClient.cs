using ZaloOA.Application.DTOs.Zalo;

namespace ZaloOA.Application.Interfaces;

public interface IZaloApiClient
{
    Task<ZaloTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri, string? codeVerifier = null);
    Task<ZaloTokenResponse> RefreshAccessTokenAsync(string refreshToken);
    Task<ZaloOAInfoResponse> GetOAInfoAsync(string accessToken);

    // Messaging APIs
    Task<ZaloFollowersResponse> GetFollowersAsync(string accessToken, int offset, int count);
    Task<ZaloUserProfileResponse> GetUserProfileAsync(string accessToken, string userId);
    Task<ZaloConversationResponse> GetConversationAsync(string accessToken, string userId, int offset, int count);
    Task<ZaloSendMessageResponse> SendTextMessageAsync(string accessToken, string userId, string message);
    Task<ZaloSendMessageResponse> SendImageMessageAsync(string accessToken, string userId, string imageUrl, string? message = null);
    Task<ZaloSendMessageResponse> SendFileMessageAsync(string accessToken, string userId, string attachmentId);
}
