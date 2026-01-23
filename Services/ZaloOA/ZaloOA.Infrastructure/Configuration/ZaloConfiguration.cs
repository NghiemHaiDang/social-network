using ZaloOA.Application.Common.Interfaces;

namespace ZaloOA.Infrastructure.Configuration;

public class ZaloConfiguration : IZaloConfiguration
{
    public string AppId { get; set; } = null!;
    public string AppSecret { get; set; } = null!;
    public string OAuthAuthorizeUrl { get; set; } = "https://oauth.zaloapp.com/v4/oa/permission";
    public string OAuthTokenUrl { get; set; } = "https://oauth.zaloapp.com/v4/oa/access_token";
    public string OpenApiBaseUrl { get; set; } = "https://openapi.zalo.me";
    public string DefaultRedirectUri { get; set; } = null!;
}
