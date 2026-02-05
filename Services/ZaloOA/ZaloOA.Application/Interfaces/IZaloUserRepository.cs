using ZaloOA.Domain.Entities;

namespace ZaloOA.Application.Interfaces;

public interface IZaloUserRepository : IRepository<ZaloUser>
{
    Task<ZaloUser?> GetByZaloUserIdAndOAIdAsync(string zaloUserId, string oaId);
    Task<IEnumerable<ZaloUser>> GetByOAIdAsync(string oaId, int offset, int limit);
    Task<int> CountByOAIdAsync(string oaId);
}
