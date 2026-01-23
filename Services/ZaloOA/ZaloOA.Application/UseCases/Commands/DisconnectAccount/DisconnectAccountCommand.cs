using ZaloOA.Application.Common.Interfaces;

namespace ZaloOA.Application.UseCases.Commands.DisconnectAccount;

public record DisconnectAccountCommand(string UserId, Guid AccountId) : ICommand;
