using ZaloOA.Application.Common;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Queries.GetAccountById;

public class GetAccountByIdHandler : IQueryHandler<GetAccountByIdQuery, ZaloOAAccountDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ZaloOAAccountDto>> HandleAsync(GetAccountByIdQuery query, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.ZaloOAAccounts.GetByIdAndUserIdAsync(query.AccountId, query.UserId, cancellationToken);

        if (account == null)
        {
            return Result<ZaloOAAccountDto>.Failure("Zalo OA account not found");
        }

        return Result<ZaloOAAccountDto>.Success(ZaloOAAccountDto.FromEntity(account));
    }
}
