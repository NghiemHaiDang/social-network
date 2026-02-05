using ZaloOA.Domain.Common;
using ZaloOA.Domain.Exceptions;

namespace ZaloOA.Domain.Entities;

public class ZaloUser : BaseEntity
{
    public string ZaloUserId { get; private set; } = null!;
    public string OAId { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsFollower { get; private set; }
    public DateTime? LastInteractionAt { get; private set; }
    public DateTime? FollowedAt { get; private set; }

    private ZaloUser() { }

    public static ZaloUser Create(
        string zaloUserId,
        string oaId,
        string? displayName = null,
        string? avatarUrl = null,
        bool isFollower = false)
    {
        if (string.IsNullOrWhiteSpace(zaloUserId))
            throw new DomainException("Zalo User ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(oaId))
            throw new DomainException("OA ID cannot be empty.");

        return new ZaloUser
        {
            ZaloUserId = zaloUserId,
            OAId = oaId,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            IsFollower = isFollower,
            FollowedAt = isFollower ? DateTime.UtcNow : null
        };
    }

    public void UpdateProfile(string? displayName, string? avatarUrl)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        SetUpdatedAt();
    }

    public void SetFollower(bool isFollower)
    {
        IsFollower = isFollower;
        if (isFollower && !FollowedAt.HasValue)
        {
            FollowedAt = DateTime.UtcNow;
        }
        SetUpdatedAt();
    }

    public void UpdateLastInteraction()
    {
        LastInteractionAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
