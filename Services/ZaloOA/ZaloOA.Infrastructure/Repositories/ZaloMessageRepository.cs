using MongoDB.Driver;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloMessageRepository : Repository<ZaloMessage>, IZaloMessageRepository
{
    public ZaloMessageRepository(MongoDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ZaloMessage>> GetByConversationIdAsync(Guid conversationId, int offset, int limit)
    {
        return await _collection
            .Find(x => x.ConversationId == conversationId)
            .SortBy(x => x.SentAt)
            .Skip(offset)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<int> CountByConversationIdAsync(Guid conversationId)
    {
        return (int)await _collection.CountDocumentsAsync(x => x.ConversationId == conversationId);
    }

    public async Task<ZaloMessage?> GetByZaloMessageIdAsync(string zaloMessageId)
    {
        return await _collection
            .Find(x => x.ZaloMessageId == zaloMessageId)
            .FirstOrDefaultAsync();
    }
}
