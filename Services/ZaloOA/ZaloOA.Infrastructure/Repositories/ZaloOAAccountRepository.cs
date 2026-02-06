using MongoDB.Driver;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloOAAccountRepository : Repository<ZaloOAAccount>, IZaloOAAccountRepository
{
    public ZaloOAAccountRepository(MongoDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ZaloOAAccount>> GetByUserIdAsync(string userId)
    {
        return await _collection
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<ZaloOAAccount?> GetByUserIdAndOAIdAsync(string userId, string oaId)
    {
        return await _collection
            .Find(x => x.UserId == userId && x.OAId == oaId)
            .FirstOrDefaultAsync();
    }

    public async Task<ZaloOAAccount?> GetByIdAndUserIdAsync(Guid id, string userId)
    {
        return await _collection
            .Find(x => x.Id == id && x.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
