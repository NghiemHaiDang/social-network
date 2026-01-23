using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Queries.GetAccountById;

public record GetAccountByIdQuery(string UserId, Guid AccountId) : IQuery<ZaloOAAccountDto>;
