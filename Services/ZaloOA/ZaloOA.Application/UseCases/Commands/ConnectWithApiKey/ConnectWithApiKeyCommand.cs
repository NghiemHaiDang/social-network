using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Commands.ConnectWithApiKey;

public record ConnectWithApiKeyCommand(
    string UserId,
    string AccessToken,
    string? RefreshToken
) : ICommand<ZaloOAAccountDto>;
