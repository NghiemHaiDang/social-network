namespace ZaloOA.Application.DTOs.ZaloOA;

public class OAuthCallbackResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? AccountId { get; set; }
    public string? OAName { get; set; }
    public string RedirectUrl { get; set; } = null!;
}
