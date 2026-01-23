namespace ZaloOA.Application.Common.Interfaces;

public interface IZaloApiService
{
    Task<ZaloTokenResult> ExchangeCodeForTokenAsync(string code, string? codeVerifier = null, CancellationToken cancellationToken = default);
    Task<ZaloTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ZaloOAInfoResult> GetOAInfoAsync(string accessToken, CancellationToken cancellationToken = default);
}

public class ZaloTokenResult
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int? ExpiresIn { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ZaloOAInfoResult
{
    public bool IsSuccess { get; set; }
    public string? OAId { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public string? ErrorMessage { get; set; }
}
