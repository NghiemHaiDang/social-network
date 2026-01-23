using ZaloOA.Application.Common;
using ZaloOA.Application.Common.Interfaces;

namespace ZaloOA.Application.UseCases.Commands.DisconnectAccount;

public class DisconnectAccountHandler : ICommandHandler<DisconnectAccountCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DisconnectAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DisconnectAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(command.AccountId, command.UserId, cancellationToken);

        if (account == null)
        {
            return Result.Failure("Zalo OA account not found");
        }

        _unitOfWork.ZaloOAAccounts.Remove(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
