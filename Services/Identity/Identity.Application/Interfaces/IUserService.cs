using Identity.Application.Common;
using Identity.Application.DTOs.User;

namespace Identity.Application.Interfaces;

public interface IUserService
{
    Task<Result<ProfileResponse>> GetProfileAsync(string userId);
    Task<Result<ProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request);
}
