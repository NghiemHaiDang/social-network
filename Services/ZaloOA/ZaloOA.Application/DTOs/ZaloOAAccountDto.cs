using ZaloOA.Domain.Entities;
using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.DTOs;

public class ZaloOAAccountDto
{
    public Guid Id { get; set; }
    public string OAId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public AuthenticationType AuthType { get; set; }
    public OAStatus Status { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ZaloOAAccountDto FromEntity(ZaloOAAccount account)
    {
        return new ZaloOAAccountDto
        {
            Id = account.Id,
            OAId = account.OAId,
            Name = account.Name,
            AvatarUrl = account.AvatarUrl,
            AuthType = account.AuthType,
            Status = account.Status,
            TokenExpiresAt = account.TokenExpiresAt,
            CreatedAt = account.CreatedAt
        };
    }
}

public class ZaloOAAccountListDto
{
    public List<ZaloOAAccountDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class OAuth2AuthorizeUrlDto
{
    public string AuthorizeUrl { get; set; } = null!;
    public string State { get; set; } = null!;
}
