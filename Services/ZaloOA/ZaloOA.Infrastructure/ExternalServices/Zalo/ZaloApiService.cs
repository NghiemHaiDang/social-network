using System.Text.Json;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Infrastructure.Configuration;
using ZaloOA.Infrastructure.ExternalServices.Zalo.Models;

namespace ZaloOA.Infrastructure.ExternalServices.Zalo;

public class ZaloApiService : IZaloApiService
{
    private readonly HttpClient _httpClient;
    private readonly ZaloConfiguration _configuration;

    public ZaloApiService(HttpClient httpClient, ZaloConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<ZaloTokenResult> ExchangeCodeForTokenAsync(string code, string? codeVerifier = null, CancellationToken cancellationToken = default)
    {
        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["app_id"] = _configuration.AppId,
            ["grant_type"] = "authorization_code"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, _configuration.OAuthTokenUrl)
        {
            Content = requestContent
        };
        request.Headers.Add("secret_key", _configuration.AppSecret);

        if (!string.IsNullOrEmpty(codeVerifier))
        {
            request.Headers.Add("code_verifier", codeVerifier);
        }

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ZaloTokenResponse>(content);

            if (result == null)
            {
                return new ZaloTokenResult { IsSuccess = false, ErrorMessage = "Failed to parse response" };
            }

            if (result.Error.HasValue && result.Error != 0)
            {
                return new ZaloTokenResult { IsSuccess = false, ErrorMessage = result.Message ?? "Token exchange failed" };
            }

            return new ZaloTokenResult
            {
                IsSuccess = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.ExpiresIn
            };
        }
        catch (Exception ex)
        {
            return new ZaloTokenResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ZaloTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["app_id"] = _configuration.AppId,
            ["grant_type"] = "refresh_token"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, _configuration.OAuthTokenUrl)
        {
            Content = requestContent
        };
        request.Headers.Add("secret_key", _configuration.AppSecret);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ZaloTokenResponse>(content);

            if (result == null)
            {
                return new ZaloTokenResult { IsSuccess = false, ErrorMessage = "Failed to parse response" };
            }

            if (result.Error.HasValue && result.Error != 0)
            {
                return new ZaloTokenResult { IsSuccess = false, ErrorMessage = result.Message ?? "Token refresh failed" };
            }

            return new ZaloTokenResult
            {
                IsSuccess = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.ExpiresIn
            };
        }
        catch (Exception ex)
        {
            return new ZaloTokenResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ZaloOAInfoResult> GetOAInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_configuration.OpenApiBaseUrl}/v2.0/oa/getoa");
        request.Headers.Add("access_token", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ZaloOAInfoResponse>(content);

            if (result == null)
            {
                return new ZaloOAInfoResult { IsSuccess = false, ErrorMessage = "Failed to parse response" };
            }

            if (result.Error != 0 || result.Data == null)
            {
                return new ZaloOAInfoResult { IsSuccess = false, ErrorMessage = result.Message ?? "Failed to get OA info" };
            }

            return new ZaloOAInfoResult
            {
                IsSuccess = true,
                OAId = result.Data.OAId,
                Name = result.Data.Name,
                Avatar = result.Data.Avatar
            };
        }
        catch (Exception ex)
        {
            return new ZaloOAInfoResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}
