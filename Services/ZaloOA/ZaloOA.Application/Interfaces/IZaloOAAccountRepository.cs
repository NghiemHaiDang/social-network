using ZaloOA.Domain.Entities;

namespace ZaloOA.Application.Interfaces;

public interface IZaloOAAccountRepository : IRepository<ZaloOAAccount>
{
    Task<IEnumerable<ZaloOAAccount>> GetByUserIdAsync(string userId);
    Task<ZaloOAAccount?> GetByUserIdAndOAIdAsync(string userId, string oaId);
    Task<ZaloOAAccount?> GetByIdAndUserIdAsync(Guid id, string userId);
}
