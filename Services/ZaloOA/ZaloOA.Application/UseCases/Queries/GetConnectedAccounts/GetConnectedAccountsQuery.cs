using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Queries.GetConnectedAccounts;

public record GetConnectedAccountsQuery(string UserId) : IQuery<ZaloOAAccountListDto>;
