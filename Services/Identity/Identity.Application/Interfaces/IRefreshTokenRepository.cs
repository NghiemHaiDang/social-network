using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAllUserTokensAsync(string userId);
}
