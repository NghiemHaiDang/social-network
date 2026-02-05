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

    public async Task<ZaloTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri, string? codeVerifier = null)
    {
        try
        {
            var formData = new Dictionary<string, string>
            {
                ["code"] = code,
                ["app_id"] = _settings.AppId,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            };

            var requestContent = new FormUrlEncodedContent(formData);

            var request = new HttpRequestMessage(HttpMethod.Post, _settings.OAuthTokenUrl)
            {
                Content = requestContent
            };
            request.Headers.Add("secret_key", _settings.AppSecret);

            if (!string.IsNullOrEmpty(codeVerifier))
            {
                request.Headers.Add("code_verifier", codeVerifier);
            }

            Console.WriteLine($"[DEBUG] ========== ExchangeCodeForToken ==========");
            Console.WriteLine($"[DEBUG] URL: {_settings.OAuthTokenUrl}");
            Console.WriteLine($"[DEBUG] AppId: {_settings.AppId}");
            Console.WriteLine($"[DEBUG] RedirectUri: {redirectUri}");
            Console.WriteLine($"[DEBUG] Code: {code?.Substring(0, Math.Min(30, code?.Length ?? 0))}...");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[DEBUG] HTTP Status: {response.StatusCode}");
            Console.WriteLine($"[DEBUG] Response: {content}");
            Console.WriteLine($"[DEBUG] ============================================");

            var result = JsonSerializer.Deserialize<ZaloTokenResponse>(content);
            return result ?? new ZaloTokenResponse { Error = -1, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] ExchangeCodeForToken Exception: {ex.Message}");
            return new ZaloTokenResponse { Error = -1, Message = ex.Message };
        }
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

    // API: Get OA Info
    public async Task<ZaloOAInfoResponse> GetOAInfoAsync(string accessToken)
    {
        try
        {
            var url = $"{_settings.OpenApiBaseUrl}/v2.0/oa/getoa";
            Console.WriteLine($"[DEBUG] ========== GetOAInfo ==========");
            Console.WriteLine($"[DEBUG] URL: {url}");
            Console.WriteLine($"[DEBUG] AccessToken: {accessToken?.Substring(0, Math.Min(30, accessToken?.Length ?? 0))}...");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("access_token", accessToken);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[DEBUG] HTTP Status: {response.StatusCode}");
            Console.WriteLine($"[DEBUG] Response: {content}");
            Console.WriteLine($"[DEBUG] ==================================");

            var result = JsonSerializer.Deserialize<ZaloOAInfoResponse>(content);
            return result ?? new ZaloOAInfoResponse { Error = -1, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] GetOAInfo Exception: {ex.Message}");
            return new ZaloOAInfoResponse { Error = -1, Message = ex.Message };
        }
    }

    // API v3: Get list of followers - POST method with body
    public async Task<ZaloFollowersResponse> GetFollowersAsync(string accessToken, int offset, int count)
    {
        var payload = new { offset, count };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.OpenApiBaseUrl}/v3.0/oa/user/getlist")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("access_token", accessToken);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[DEBUG API v3] GetFollowers Response: {content}");

        var result = JsonSerializer.Deserialize<ZaloFollowersResponse>(content);
        return result ?? new ZaloFollowersResponse { Error = -1, Message = "Failed to parse response" };
    }

    // API v3: Get user profile - POST with JSON body
    public async Task<ZaloUserProfileResponse> GetUserProfileAsync(string accessToken, string userId)
    {
        var payload = new { user_id = userId };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.OpenApiBaseUrl}/v3.0/oa/user/detail")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("access_token", accessToken);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[DEBUG API v3] GetUserProfile Response: {content}");

        var result = JsonSerializer.Deserialize<ZaloUserProfileResponse>(content);
        return result ?? new ZaloUserProfileResponse { Error = -1, Message = "Failed to parse response" };
    }

    // API v3: Get conversation history
    public async Task<ZaloConversationResponse> GetConversationAsync(string accessToken, string userId, int offset, int count)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_settings.OpenApiBaseUrl}/v3.0/oa/conversation?user_id={userId}&offset={offset}&count={count}");
        request.Headers.Add("access_token", accessToken);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[DEBUG API v3] GetConversation Response: {content}");

        var result = JsonSerializer.Deserialize<ZaloConversationResponse>(content);
        return result ?? new ZaloConversationResponse { Error = -1, Message = "Failed to parse response" };
    }

    // API v3: Send text message (customer service)
    public async Task<ZaloSendMessageResponse> SendTextMessageAsync(string accessToken, string userId, string message)
    {
        var payload = new
        {
            recipient = new { user_id = userId },
            message = new { text = message }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.OpenApiBaseUrl}/v3.0/oa/message/cs")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("access_token", accessToken);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[DEBUG API v3] SendTextMessage Response: {content}");

        var result = JsonSerializer.Deserialize<ZaloSendMessageResponse>(content);
        return result ?? new ZaloSendMessageResponse { Error = -1, Message = "Failed to parse response" };
    }

    // API v3: Send image message
    public async Task<ZaloSendMessageResponse> SendImageMessageAsync(string accessToken, string userId, string imageUrl, string? message = null)
    {
        var messagePayload = new Dictionary<string, object>
        {
            ["attachment"] = new
            {
                type = "template",
                payload = new
                {
                    template_type = "media",
                    elements = new[]
                    {
                        new
                        {
                            media_type = "image",
                            url = imageUrl
                        }
                    }
                }
            }
        };

        if (!string.IsNullOrEmpty(message))
        {
            messagePayload["text"] = message;
        }

        var payload = new
        {
            recipient = new { user_id = userId },
            message = messagePayload
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.OpenApiBaseUrl}/v3.0/oa/message/cs")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("access_token", accessToken);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ZaloSendMessageResponse>(content);
        return result ?? new ZaloSendMessageResponse { Error = -1, Message = "Failed to parse response" };
    }

    // API v3: Send file message
    public async Task<ZaloSendMessageResponse> SendFileMessageAsync(string accessToken, string userId, string attachmentId)
    {
        var payload = new
        {
            recipient = new { user_id = userId },
            message = new
            {
                attachment = new
                {
                    type = "file",
                    payload = new { attachment_id = attachmentId }
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.OpenApiBaseUrl}/v3.0/oa/message/cs")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("access_token", accessToken);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ZaloSendMessageResponse>(content);
        return result ?? new ZaloSendMessageResponse { Error = -1, Message = "Failed to parse response" };
    }
}
