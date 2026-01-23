using Microsoft.EntityFrameworkCore;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloOAAccountRepository : Repository<ZaloOAAccount>, IZaloOAAccountRepository
{
    public ZaloOAAccountRepository(ZaloOADbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ZaloOAAccount>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<ZaloOAAccount?> GetByUserIdAndOAIdAsync(string userId, string oaId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.UserId == userId && x.OAId == oaId);
    }

    public async Task<ZaloOAAccount?> GetByIdAndUserIdAsync(Guid id, string userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    }
}
