namespace ZaloOA.Application.Common.Interfaces;

public interface IZaloConfiguration
{
    string AppId { get; }
    string AppSecret { get; }
    string OAuthAuthorizeUrl { get; }
    string OAuthTokenUrl { get; }
    string OpenApiBaseUrl { get; }
    string DefaultRedirectUri { get; }
}
