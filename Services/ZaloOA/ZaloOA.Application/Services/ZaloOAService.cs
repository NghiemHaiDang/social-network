using ZaloOA.Application.Common;
using ZaloOA.Application.DTOs.ZaloOA;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Domain.Enums;

namespace ZaloOA.Application.Services;

public class ZaloOAService : IZaloOAService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IZaloApiClient _zaloApiClient;
    private readonly ZaloSettings _zaloSettings;

    public ZaloOAService(
        IUnitOfWork unitOfWork,
        IZaloApiClient zaloApiClient,
        ZaloSettings zaloSettings)
    {
        _unitOfWork = unitOfWork;
        _zaloApiClient = zaloApiClient;
        _zaloSettings = zaloSettings;
    }

    public Task<Result<OAuth2AuthorizeUrlResponse>> GetOAuth2AuthorizeUrlAsync(string? redirectUri = null)
    {
        var state = Guid.NewGuid().ToString("N");
        var finalRedirectUri = redirectUri ?? _zaloSettings.DefaultRedirectUri;

        var authorizeUrl = $"{_zaloSettings.OAuthAuthorizeUrl}" +
            $"?app_id={_zaloSettings.AppId}" +
            $"&redirect_uri={Uri.EscapeDataString(finalRedirectUri)}" +
            $"&state={state}";

        var response = new OAuth2AuthorizeUrlResponse
        {
            AuthorizeUrl = authorizeUrl,
            State = state
        };

        return Task.FromResult(Result<OAuth2AuthorizeUrlResponse>.Success(response));
    }

    public async Task<Result<ZaloOAResponse>> ConnectWithOAuth2Async(string userId, ConnectOAuth2Request request)
    {
        var tokenResponse = await _zaloApiClient.ExchangeCodeForTokenAsync(request.Code, request.CodeVerifier);

        if (tokenResponse.Error.HasValue && tokenResponse.Error != 0)
        {
            return Result<ZaloOAResponse>.Failure(tokenResponse.Message ?? "Failed to exchange code for token");
        }

        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            return Result<ZaloOAResponse>.Failure("No access token received from Zalo");
        }

        var oaInfoResponse = await _zaloApiClient.GetOAInfoAsync(tokenResponse.AccessToken);

        if (oaInfoResponse.Error != 0 || oaInfoResponse.Data == null)
        {
            return Result<ZaloOAResponse>.Failure(oaInfoResponse.Message ?? "Failed to get OA information");
        }

        var oaId = oaInfoResponse.Data.OAId!;

        var existingAccount = await _unitOfWork.ZaloOAAccounts.GetByUserIdAndOAIdAsync(userId, oaId);
        if (existingAccount != null)
        {
            existingAccount.AccessToken = tokenResponse.AccessToken;
            existingAccount.RefreshToken = tokenResponse.RefreshToken;
            existingAccount.TokenExpiresAt = tokenResponse.ExpiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
                : null;
            existingAccount.Status = OAStatus.Active;
            existingAccount.Name = oaInfoResponse.Data.Name ?? existingAccount.Name;
            existingAccount.AvatarUrl = oaInfoResponse.Data.Avatar;
            existingAccount.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.ZaloOAAccounts.Update(existingAccount);
            await _unitOfWork.SaveChangesAsync();

            return Result<ZaloOAResponse>.Success(MapToResponse(existingAccount));
        }

        var newAccount = new ZaloOAAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OAId = oaId,
            Name = oaInfoResponse.Data.Name ?? "Unknown OA",
            AvatarUrl = oaInfoResponse.Data.Avatar,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenExpiresAt = tokenResponse.ExpiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
                : null,
            AuthType = AuthenticationType.OAuth2,
            Status = OAStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ZaloOAAccounts.AddAsync(newAccount);
        await _unitOfWork.SaveChangesAsync();

        return Result<ZaloOAResponse>.Success(MapToResponse(newAccount));
    }

    public async Task<Result<ZaloOAResponse>> ConnectWithApiKeyAsync(string userId, ConnectApiKeyRequest request)
    {
        var oaInfoResponse = await _zaloApiClient.GetOAInfoAsync(request.AccessToken);

        if (oaInfoResponse.Error != 0 || oaInfoResponse.Data == null)
        {
            return Result<ZaloOAResponse>.Failure(oaInfoResponse.Message ?? "Invalid access token or failed to get OA information");
        }

        var oaId = oaInfoResponse.Data.OAId!;

        var existingAccount = await _unitOfWork.ZaloOAAccounts.GetByUserIdAndOAIdAsync(userId, oaId);
        if (existingAccount != null)
        {
            existingAccount.AccessToken = request.AccessToken;
            existingAccount.RefreshToken = request.RefreshToken;
            existingAccount.Status = OAStatus.Active;
            existingAccount.Name = oaInfoResponse.Data.Name ?? existingAccount.Name;
            existingAccount.AvatarUrl = oaInfoResponse.Data.Avatar;
            existingAccount.AuthType = AuthenticationType.ApiKey;
            existingAccount.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.ZaloOAAccounts.Update(existingAccount);
            await _unitOfWork.SaveChangesAsync();

            return Result<ZaloOAResponse>.Success(MapToResponse(existingAccount));
        }

        var newAccount = new ZaloOAAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OAId = oaId,
            Name = oaInfoResponse.Data.Name ?? "Unknown OA",
            AvatarUrl = oaInfoResponse.Data.Avatar,
            AccessToken = request.AccessToken,
            RefreshToken = request.RefreshToken,
            AuthType = AuthenticationType.ApiKey,
            Status = OAStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ZaloOAAccounts.AddAsync(newAccount);
        await _unitOfWork.SaveChangesAsync();

        return Result<ZaloOAResponse>.Success(MapToResponse(newAccount));
    }

    public async Task<Result<ZaloOAListResponse>> GetConnectedAccountsAsync(string userId)
    {
        var accounts = await _unitOfWork.ZaloOAAccounts.GetByUserIdAsync(userId);

        var response = new ZaloOAListResponse
        {
            Items = accounts.Select(MapToResponse).ToList(),
            TotalCount = accounts.Count()
        };

        return Result<ZaloOAListResponse>.Success(response);
    }

    public async Task<Result<ZaloOAResponse>> GetAccountByIdAsync(string userId, Guid id)
    {
        var account = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(id, userId);

        if (account == null)
        {
            return Result<ZaloOAResponse>.Failure("Zalo OA account not found");
        }

        return Result<ZaloOAResponse>.Success(MapToResponse(account));
    }

    public async Task<Result> DisconnectAccountAsync(string userId, Guid id)
    {
        var account = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(id, userId);

        if (account == null)
        {
            return Result.Failure("Zalo OA account not found");
        }

        _unitOfWork.ZaloOAAccounts.Remove(account);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<ZaloOAResponse>> RefreshTokenAsync(string userId, Guid id)
    {
        var account = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(id, userId);

        if (account == null)
        {
            return Result<ZaloOAResponse>.Failure("Zalo OA account not found");
        }

        if (string.IsNullOrEmpty(account.RefreshToken))
        {
            return Result<ZaloOAResponse>.Failure("No refresh token available for this account");
        }

        var tokenResponse = await _zaloApiClient.RefreshAccessTokenAsync(account.RefreshToken);

        if (tokenResponse.Error.HasValue && tokenResponse.Error != 0)
        {
            account.Status = OAStatus.TokenExpired;
            _unitOfWork.ZaloOAAccounts.Update(account);
            await _unitOfWork.SaveChangesAsync();

            return Result<ZaloOAResponse>.Failure(tokenResponse.Message ?? "Failed to refresh token");
        }

        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            return Result<ZaloOAResponse>.Failure("No access token received from Zalo");
        }

        account.AccessToken = tokenResponse.AccessToken;
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            account.RefreshToken = tokenResponse.RefreshToken;
        }
        account.TokenExpiresAt = tokenResponse.ExpiresIn.HasValue
            ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
            : null;
        account.Status = OAStatus.Active;
        account.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ZaloOAAccounts.Update(account);
        await _unitOfWork.SaveChangesAsync();

        return Result<ZaloOAResponse>.Success(MapToResponse(account));
    }

    private static ZaloOAResponse MapToResponse(ZaloOAAccount account)
    {
        return new ZaloOAResponse
        {
            Id = account.Id,
            OAId = account.OAId,
            Name = account.Name,
            AvatarUrl = account.AvatarUrl,
            AuthType = account.AuthType,
            Status = account.Status,
            TokenExpiresAt = account.TokenExpiresAt,
            CreatedAt = account.CreatedAt
        };
    }
}

public class ZaloSettings
{
    public string AppId { get; set; } = null!;
    public string AppSecret { get; set; } = null!;
    public string OAuthAuthorizeUrl { get; set; } = "https://oauth.zaloapp.com/v4/oa/permission";
    public string OAuthTokenUrl { get; set; } = "https://oauth.zaloapp.com/v4/oa/access_token";
    public string OpenApiBaseUrl { get; set; } = "https://openapi.zalo.me";
    public string DefaultRedirectUri { get; set; } = null!;
}
