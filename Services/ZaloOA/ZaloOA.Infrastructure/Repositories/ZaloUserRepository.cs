using Microsoft.EntityFrameworkCore;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloUserRepository : Repository<ZaloUser>, IZaloUserRepository
{
    public ZaloUserRepository(ZaloOADbContext context) : base(context)
    {
    }

    public async Task<ZaloUser?> GetByZaloUserIdAndOAIdAsync(string zaloUserId, string oaId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.ZaloUserId == zaloUserId && x.OAId == oaId);
    }

    public async Task<IEnumerable<ZaloUser>> GetByOAIdAsync(string oaId, int offset, int limit)
    {
        return await _dbSet
            .Where(x => x.OAId == oaId)
            .OrderByDescending(x => x.LastInteractionAt ?? x.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CountByOAIdAsync(string oaId)
    {
        return await _dbSet.CountAsync(x => x.OAId == oaId);
    }
}
