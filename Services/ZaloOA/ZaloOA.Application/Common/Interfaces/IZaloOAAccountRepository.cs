using ZaloOA.Domain.Entities;

namespace ZaloOA.Application.Common.Interfaces;

public interface IZaloOAAccountRepository : IRepository<ZaloOAAccount>
{
    Task<IEnumerable<ZaloOAAccount>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ZaloOAAccount?> GetByUserIdAndOAIdAsync(string userId, string oaId, CancellationToken cancellationToken = default);
    Task<ZaloOAAccount?> GetByIdAndUserIdAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
