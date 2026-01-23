using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.DTOs.ZaloOA;

public class ZaloOAResponse
{
    public Guid Id { get; set; }
    public string OAId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public AuthenticationType AuthType { get; set; }
    public OAStatus Status { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
