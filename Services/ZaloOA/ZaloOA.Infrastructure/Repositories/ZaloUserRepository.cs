using MongoDB.Driver;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloUserRepository : Repository<ZaloUser>, IZaloUserRepository
{
    public ZaloUserRepository(MongoDbContext context) : base(context)
    {
    }

    public async Task<ZaloUser?> GetByZaloUserIdAndOAIdAsync(string zaloUserId, string oaId)
    {
        return await _collection
            .Find(x => x.ZaloUserId == zaloUserId && x.OAId == oaId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ZaloUser>> GetByOAIdAsync(string oaId, int offset, int limit)
    {
        return await _collection
            .Find(x => x.OAId == oaId)
            .SortByDescending(x => x.LastInteractionAt ?? x.CreatedAt)
            .Skip(offset)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<int> CountByOAIdAsync(string oaId)
    {
        return (int)await _collection.CountDocumentsAsync(x => x.OAId == oaId);
    }
}
