using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Commands.RefreshToken;

public record RefreshTokenCommand(string UserId, Guid AccountId) : ICommand<ZaloOAAccountDto>;
