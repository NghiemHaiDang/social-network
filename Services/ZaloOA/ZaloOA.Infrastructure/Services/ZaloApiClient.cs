using System.Text.Json;
using ZaloOA.Application.DTOs.Zalo;
using ZaloOA.Application.Interfaces;
using ZaloOA.Application.Services;

namespace ZaloOA.Infrastructure.Services;

public class ZaloApiClient : IZaloApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ZaloSettings _settings;

    public ZaloApiClient(HttpClient httpClient, ZaloSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<ZaloTokenResponse> ExchangeCodeForTokenAsync(string code, string? codeVerifier = null)
    {
        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["app_id"] = _settings.AppId,
            ["grant_type"] = "authorization_code"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.OAuthTokenUrl)
        {
            Content = requestContent
        };
        request.Headers.Add("secret_key", _settings.AppSecret);

        if (!string.IsNullOrEmpty(codeVerifier))
        {
            request.Headers.Add("code_verifier", codeVerifier);
        }

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ZaloTokenResponse>(content);
        return result ?? new ZaloTokenResponse { Error = -1, Message = "Failed to parse response" };
    }

    public async Task<ZaloTokenResponse> RefreshAccessTokenAsync(string refreshToken)
    {
        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["app_id"] = _settings.AppId,
            ["grant_type"] = "refresh_token"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.OAuthTokenUrl)
        {
            Content = requestContent
        };
        request.Headers.Add("secret_key", _settings.AppSecret);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ZaloTokenResponse>(content);
        return result ?? new ZaloTokenResponse { Error = -1, Message = "Failed to parse response" };
    }

    public async Task<ZaloOAInfoResponse> GetOAInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.OpenApiBaseUrl}/v2.0/oa/getoa");
        request.Headers.Add("access_token", accessToken);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ZaloOAInfoResponse>(content);
        return result ?? new ZaloOAInfoResponse { Error = -1, Message = "Failed to parse response" };
    }
}
