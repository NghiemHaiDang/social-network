using Identity.Application.Common;
using Identity.Application.DTOs.Auth;

namespace Identity.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken);
    Task<Result<bool>> RevokeTokenAsync(string userId);
}
