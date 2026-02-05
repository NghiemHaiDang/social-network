using Microsoft.EntityFrameworkCore;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloMessageRepository : Repository<ZaloMessage>, IZaloMessageRepository
{
    public ZaloMessageRepository(ZaloOADbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ZaloMessage>> GetByConversationIdAsync(Guid conversationId, int offset, int limit)
    {
        return await _dbSet
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.SentAt)  // Cũ nhất lên đầu, mới nhất ở cuối
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CountByConversationIdAsync(Guid conversationId)
    {
        return await _dbSet.CountAsync(x => x.ConversationId == conversationId);
    }

    public async Task<ZaloMessage?> GetByZaloMessageIdAsync(string zaloMessageId)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.ZaloMessageId == zaloMessageId);
    }
}
