using Microsoft.EntityFrameworkCore;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloConversationRepository : Repository<ZaloConversation>, IZaloConversationRepository
{
    public ZaloConversationRepository(ZaloOADbContext context) : base(context)
    {
    }

    public async Task<ZaloConversation?> GetByOAAccountIdAndZaloUserIdAsync(Guid oaAccountId, Guid zaloUserId)
    {
        return await _dbSet
            .Include(x => x.ZaloUser)
            .FirstOrDefaultAsync(x => x.OAAccountId == oaAccountId && x.ZaloUserId == zaloUserId);
    }

    public async Task<IEnumerable<ZaloConversation>> GetByOAAccountIdAsync(Guid oaAccountId, int offset, int limit)
    {
        return await _dbSet
            .Include(x => x.ZaloUser)
            .Where(x => x.OAAccountId == oaAccountId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ZaloConversation?> GetWithMessagesAsync(Guid id, int messageOffset, int messageLimit)
    {
        var conversation = await _dbSet
            .Include(x => x.ZaloUser)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (conversation != null)
        {
            await _context.Entry(conversation)
                .Collection(x => x.Messages)
                .Query()
                .OrderByDescending(m => m.SentAt)
                .Skip(messageOffset)
                .Take(messageLimit)
                .LoadAsync();
        }

        return conversation;
    }

    public async Task<int> CountByOAAccountIdAsync(Guid oaAccountId)
    {
        return await _dbSet.CountAsync(x => x.OAAccountId == oaAccountId);
    }
}
