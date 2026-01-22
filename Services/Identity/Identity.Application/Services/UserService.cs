using Microsoft.AspNetCore.Identity;
using Identity.Application.Common;
using Identity.Application.DTOs.User;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;

namespace Identity.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(UserManager<User> userManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfileResponse>> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<ProfileResponse>.Failure("User not found");
        }

        var profile = new ProfileResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Result<ProfileResponse>.Success(profile);
    }

    public async Task<Result<ProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<ProfileResponse>.Failure("User not found");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;

            if (request.PhoneNumber != null)
                user.PhoneNumber = request.PhoneNumber;

            if (request.AvatarUrl != null)
                user.AvatarUrl = request.AvatarUrl;

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result<ProfileResponse>.Failure(errors);
            }

            await _unitOfWork.CommitTransactionAsync();
            return await GetProfileAsync(userId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
