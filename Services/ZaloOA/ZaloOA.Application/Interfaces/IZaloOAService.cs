using ZaloOA.Application.Common;
using ZaloOA.Application.DTOs.ZaloOA;

namespace ZaloOA.Application.Interfaces;

public interface IZaloOAService
{
    Task<Result<OAuth2AuthorizeUrlResponse>> GetOAuth2AuthorizeUrlAsync(string userId, string? redirectUri = null);
    Task<Result<OAuthCallbackResponse>> HandleOAuthCallbackAsync(string code, string state);
    Task<Result<ZaloOAResponse>> ConnectWithOAuth2Async(string userId, ConnectOAuth2Request request);
    Task<Result<ZaloOAResponse>> ConnectWithApiKeyAsync(string userId, ConnectApiKeyRequest request);
    Task<Result<ZaloOAListResponse>> GetConnectedAccountsAsync(string userId);
    Task<Result<ZaloOAResponse>> GetAccountByIdAsync(string userId, Guid id);
    Task<Result> DisconnectAccountAsync(string userId, Guid id);
    Task<Result<ZaloOAResponse>> RefreshTokenAsync(string userId, Guid id);
}
