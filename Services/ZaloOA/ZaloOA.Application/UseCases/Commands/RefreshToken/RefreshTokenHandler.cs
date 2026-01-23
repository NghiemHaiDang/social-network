using ZaloOA.Application.Common;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Commands.RefreshToken;

public class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, ZaloOAAccountDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IZaloApiService _zaloApiService;

    public RefreshTokenHandler(IUnitOfWork unitOfWork, IZaloApiService zaloApiService)
    {
        _unitOfWork = unitOfWork;
        _zaloApiService = zaloApiService;
    }

    public async Task<Result<ZaloOAAccountDto>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(command.AccountId, command.UserId, cancellationToken);

        if (account == null)
        {
            return Result<ZaloOAAccountDto>.Failure("Zalo OA account not found");
        }

        if (!account.HasRefreshToken())
        {
            return Result<ZaloOAAccountDto>.Failure("No refresh token available for this account");
        }

        var tokenResult = await _zaloApiService.RefreshAccessTokenAsync(account.RefreshToken!, cancellationToken);

        if (!tokenResult.IsSuccess || string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            account.MarkAsTokenExpired();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ZaloOAAccountDto>.Failure(tokenResult.ErrorMessage ?? "Failed to refresh token");
        }

        account.UpdateTokens(tokenResult.AccessToken, tokenResult.RefreshToken, tokenResult.ExpiresIn);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ZaloOAAccountDto>.Success(ZaloOAAccountDto.FromEntity(account));
    }
}
