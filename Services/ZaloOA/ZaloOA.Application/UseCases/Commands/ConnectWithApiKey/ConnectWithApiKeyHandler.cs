using ZaloOA.Application.Common;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;
using ZaloOA.Domain.Entities;
using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.UseCases.Commands.ConnectWithApiKey;

public class ConnectWithApiKeyHandler : ICommandHandler<ConnectWithApiKeyCommand, ZaloOAAccountDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IZaloApiService _zaloApiService;

    public ConnectWithApiKeyHandler(IUnitOfWork unitOfWork, IZaloApiService zaloApiService)
    {
        _unitOfWork = unitOfWork;
        _zaloApiService = zaloApiService;
    }

    public async Task<Result<ZaloOAAccountDto>> HandleAsync(ConnectWithApiKeyCommand command, CancellationToken cancellationToken = default)
    {
        // Validate access token by getting OA info
        var oaInfoResult = await _zaloApiService.GetOAInfoAsync(command.AccessToken, cancellationToken);

        if (!oaInfoResult.IsSuccess || string.IsNullOrEmpty(oaInfoResult.OAId))
        {
            return Result<ZaloOAAccountDto>.Failure(oaInfoResult.ErrorMessage ?? "Invalid access token or failed to get OA information");
        }

        // Check existing account
        var existingAccount = await _unitOfWork.ZaloOAAccounts.GetByUserIdAndOAIdAsync(command.UserId, oaInfoResult.OAId, cancellationToken);

        ZaloOAAccount account;

        if (existingAccount != null)
        {
            existingAccount.UpdateTokens(command.AccessToken, command.RefreshToken, null);
            existingAccount.UpdateOAInfo(oaInfoResult.Name, oaInfoResult.Avatar);
            existingAccount.UpdateAuthType(AuthenticationType.ApiKey);
            account = existingAccount;
        }
        else
        {
            account = ZaloOAAccount.Create(
                command.UserId,
                oaInfoResult.OAId,
                oaInfoResult.Name ?? "Unknown OA",
                oaInfoResult.Avatar,
                command.AccessToken,
                command.RefreshToken,
                null,
                AuthenticationType.ApiKey
            );

            await _unitOfWork.ZaloOAAccounts.AddAsync(account, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ZaloOAAccountDto>.Success(ZaloOAAccountDto.FromEntity(account));
    }
}
