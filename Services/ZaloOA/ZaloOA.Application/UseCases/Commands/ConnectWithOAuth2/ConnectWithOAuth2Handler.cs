using ZaloOA.Application.Common;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;
using ZaloOA.Domain.Entities;
using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.UseCases.Commands.ConnectWithOAuth2;

public class ConnectWithOAuth2Handler : ICommandHandler<ConnectWithOAuth2Command, ZaloOAAccountDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IZaloApiService _zaloApiService;

    public ConnectWithOAuth2Handler(IUnitOfWork unitOfWork, IZaloApiService zaloApiService)
    {
        _unitOfWork = unitOfWork;
        _zaloApiService = zaloApiService;
    }

    public async Task<Result<ZaloOAAccountDto>> HandleAsync(ConnectWithOAuth2Command command, CancellationToken cancellationToken = default)
    {
        // Exchange code for token
        var tokenResult = await _zaloApiService.ExchangeCodeForTokenAsync(command.Code, command.CodeVerifier, cancellationToken);

        if (!tokenResult.IsSuccess || string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            return Result<ZaloOAAccountDto>.Failure(tokenResult.ErrorMessage ?? "Failed to exchange code for token");
        }

        // Get OA info
        var oaInfoResult = await _zaloApiService.GetOAInfoAsync(tokenResult.AccessToken, cancellationToken);

        if (!oaInfoResult.IsSuccess || string.IsNullOrEmpty(oaInfoResult.OAId))
        {
            return Result<ZaloOAAccountDto>.Failure(oaInfoResult.ErrorMessage ?? "Failed to get OA information");
        }

        // Check existing account
        var existingAccount = await _unitOfWork.ZaloOAAccounts.GetByUserIdAndOAIdAsync(command.UserId, oaInfoResult.OAId, cancellationToken);

        ZaloOAAccount account;

        if (existingAccount != null)
        {
            existingAccount.UpdateTokens(tokenResult.AccessToken, tokenResult.RefreshToken, tokenResult.ExpiresIn);
            existingAccount.UpdateOAInfo(oaInfoResult.Name, oaInfoResult.Avatar);
            account = existingAccount;
        }
        else
        {
            account = ZaloOAAccount.Create(
                command.UserId,
                oaInfoResult.OAId,
                oaInfoResult.Name ?? "Unknown OA",
                oaInfoResult.Avatar,
                tokenResult.AccessToken,
                tokenResult.RefreshToken,
                tokenResult.ExpiresIn,
                AuthenticationType.OAuth2
            );

            await _unitOfWork.ZaloOAAccounts.AddAsync(account, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ZaloOAAccountDto>.Success(ZaloOAAccountDto.FromEntity(account));
    }
}
