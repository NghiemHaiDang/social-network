using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Commands.ConnectWithOAuth2;

public record ConnectWithOAuth2Command(
    string UserId,
    string Code,
    string? CodeVerifier
) : ICommand<ZaloOAAccountDto>;
