using Microsoft.EntityFrameworkCore;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Domain.Entities;

namespace ZaloOA.Infrastructure.Persistence.Repositories;

public class ZaloOAAccountRepository : Repository<ZaloOAAccount>, IZaloOAAccountRepository
{
    public ZaloOAAccountRepository(ZaloOADbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ZaloOAAccount>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ZaloOAAccount?> GetByUserIdAndOAIdAsync(string userId, string oaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.UserId == userId && x.OAId == oaId, cancellationToken);
    }

    public async Task<ZaloOAAccount?> GetByIdAndUserIdAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }
}
