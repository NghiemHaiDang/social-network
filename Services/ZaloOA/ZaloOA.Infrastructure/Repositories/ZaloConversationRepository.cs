using MongoDB.Driver;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Entities;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class ZaloConversationRepository : Repository<ZaloConversation>, IZaloConversationRepository
{
    private readonly MongoDbContext _dbContext;

    public ZaloConversationRepository(MongoDbContext context) : base(context)
    {
        _dbContext = context;
    }

    public async Task<ZaloConversation?> GetByOAAccountIdAndZaloUserIdAsync(Guid oaAccountId, Guid zaloUserId)
    {
        var conversation = await _collection
            .Find(x => x.OAAccountId == oaAccountId && x.ZaloUserId == zaloUserId)
            .FirstOrDefaultAsync();

        if (conversation != null)
        {
            await PopulateZaloUser(conversation);
        }

        return conversation;
    }

    public async Task<IEnumerable<ZaloConversation>> GetByOAAccountIdAsync(Guid oaAccountId, int offset, int limit)
    {
        var conversations = await _collection
            .Find(x => x.OAAccountId == oaAccountId)
            .SortByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .Skip(offset)
            .Limit(limit)
            .ToListAsync();

        foreach (var conversation in conversations)
        {
            await PopulateZaloUser(conversation);
        }

        return conversations;
    }

    public async Task<ZaloConversation?> GetWithMessagesAsync(Guid id, int messageOffset, int messageLimit)
    {
        var conversation = await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (conversation != null)
        {
            await PopulateZaloUser(conversation);

            var messages = await _dbContext.ZaloMessages
                .Find(m => m.ConversationId == id)
                .SortByDescending(m => m.SentAt)
                .Skip(messageOffset)
                .Limit(messageLimit)
                .ToListAsync();

            // Set Messages collection via the ICollection property
            foreach (var message in messages)
            {
                conversation.Messages.Add(message);
            }
        }

        return conversation;
    }

    public async Task<int> CountByOAAccountIdAsync(Guid oaAccountId)
    {
        return (int)await _collection.CountDocumentsAsync(x => x.OAAccountId == oaAccountId);
    }

    private async Task PopulateZaloUser(ZaloConversation conversation)
    {
        var zaloUser = await _dbContext.ZaloUsers
            .Find(u => u.Id == conversation.ZaloUserId)
            .FirstOrDefaultAsync();

        if (zaloUser != null)
        {
            // Use reflection to set the navigation property (private set)
            var property = typeof(ZaloConversation).GetProperty(nameof(ZaloConversation.ZaloUser));
            property?.SetValue(conversation, zaloUser);
        }
    }
}
