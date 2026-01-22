using Microsoft.AspNetCore.Identity;
using Identity.Application.Common;
using Identity.Application.DTOs.Auth;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;

namespace Identity.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<User> userManager,
        IJwtService jwtService,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure("Email already exists");
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result<AuthResponse>.Failure(errors);
            }

            var authResponse = await GenerateAuthResponseAsync(user);
            await _unitOfWork.CommitTransactionAsync();

            return authResponse;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password");
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            return Result<AuthResponse>.Failure("Invalid email or password");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Invalid or expired refresh token");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null)
        {
            return Result<AuthResponse>.Failure("User not found");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Revoke old refresh token
            storedToken.IsRevoked = true;
            _unitOfWork.RefreshTokens.Update(storedToken);

            var authResponse = await GenerateAuthResponseAsync(user);
            await _unitOfWork.CommitTransactionAsync();

            return authResponse;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result<bool>> RevokeTokenAsync(string userId)
    {
        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private async Task<Result<AuthResponse>> GenerateAuthResponseAsync(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl
            }
        };

        return Result<AuthResponse>.Success(response);
    }
}
