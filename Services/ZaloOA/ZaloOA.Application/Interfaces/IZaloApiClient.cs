using ZaloOA.Application.DTOs.Zalo;

namespace ZaloOA.Application.Interfaces;

public interface IZaloApiClient
{
    Task<ZaloTokenResponse> ExchangeCodeForTokenAsync(string code, string? codeVerifier = null);
    Task<ZaloTokenResponse> RefreshAccessTokenAsync(string refreshToken);
    Task<ZaloOAInfoResponse> GetOAInfoAsync(string accessToken);
}
