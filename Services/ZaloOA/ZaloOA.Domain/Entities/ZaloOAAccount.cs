using ZaloOA.Domain.Common;
using ZaloOA.Domain.Enums;
using ZaloOA.Domain.Exceptions;

namespace ZaloOA.Domain.Entities;

public class ZaloOAAccount : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public string OAId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public string AccessToken { get; private set; } = null!;
    public string? RefreshToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }
    public AuthenticationType AuthType { get; private set; }
    public OAStatus Status { get; private set; }

    private ZaloOAAccount() { }

    public static ZaloOAAccount Create(
        string userId,
        string oaId,
        string name,
        string? avatarUrl,
        string accessToken,
        string? refreshToken,
        int? expiresInSeconds,
        AuthenticationType authType)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(oaId))
            throw new DomainException("OA ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new DomainException("Access token cannot be empty.");

        return new ZaloOAAccount
        {
            UserId = userId,
            OAId = oaId,
            Name = string.IsNullOrWhiteSpace(name) ? "Unknown OA" : name,
            AvatarUrl = avatarUrl,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = expiresInSeconds.HasValue
                ? DateTime.UtcNow.AddSeconds(expiresInSeconds.Value)
                : null,
            AuthType = authType,
            Status = OAStatus.Active
        };
    }

    public void UpdateTokens(string accessToken, string? refreshToken, int? expiresInSeconds)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new DomainException("Access token cannot be empty.");

        AccessToken = accessToken;

        if (!string.IsNullOrEmpty(refreshToken))
        {
            RefreshToken = refreshToken;
        }

        TokenExpiresAt = expiresInSeconds.HasValue
            ? DateTime.UtcNow.AddSeconds(expiresInSeconds.Value)
            : null;

        Status = OAStatus.Active;
        SetUpdatedAt();
    }

    public void UpdateOAInfo(string? name, string? avatarUrl)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }

        AvatarUrl = avatarUrl;
        SetUpdatedAt();
    }

    public void UpdateAuthType(AuthenticationType authType)
    {
        AuthType = authType;
        SetUpdatedAt();
    }

    public void MarkAsTokenExpired()
    {
        Status = OAStatus.TokenExpired;
        SetUpdatedAt();
    }

    public void Activate()
    {
        Status = OAStatus.Active;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        Status = OAStatus.Inactive;
        SetUpdatedAt();
    }

    public bool IsTokenExpired()
    {
        return TokenExpiresAt.HasValue && TokenExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool HasRefreshToken()
    {
        return !string.IsNullOrEmpty(RefreshToken);
    }
}
