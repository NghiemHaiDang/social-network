using ZaloOA.Application.Common;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Queries.GetConnectedAccounts;

public class GetConnectedAccountsHandler : IQueryHandler<GetConnectedAccountsQuery, ZaloOAAccountListDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConnectedAccountsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ZaloOAAccountListDto>> HandleAsync(GetConnectedAccountsQuery query, CancellationToken cancellationToken = default)
    {
        var accounts = await _unitOfWork.ZaloOAAccounts.GetByUserIdAsync(query.UserId, cancellationToken);

        var response = new ZaloOAAccountListDto
        {
            Items = accounts.Select(ZaloOAAccountDto.FromEntity).ToList(),
            TotalCount = accounts.Count()
        };

        return Result<ZaloOAAccountListDto>.Success(response);
    }
}
