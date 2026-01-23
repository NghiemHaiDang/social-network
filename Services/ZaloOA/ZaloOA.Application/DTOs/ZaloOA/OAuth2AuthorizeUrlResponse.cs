namespace ZaloOA.Application.DTOs.ZaloOA;

public class OAuth2AuthorizeUrlResponse
{
    public string AuthorizeUrl { get; set; } = null!;
    public string? CodeChallenge { get; set; }
    public string? State { get; set; }
}
