using ZaloOA.Domain.Entities;

namespace ZaloOA.Application.Interfaces;

public interface IZaloConversationRepository : IRepository<ZaloConversation>
{
    Task<ZaloConversation?> GetByOAAccountIdAndZaloUserIdAsync(Guid oaAccountId, Guid zaloUserId);
    Task<IEnumerable<ZaloConversation>> GetByOAAccountIdAsync(Guid oaAccountId, int offset, int limit);
    Task<ZaloConversation?> GetWithMessagesAsync(Guid id, int messageOffset, int messageLimit);
    Task<int> CountByOAAccountIdAsync(Guid oaAccountId);
}
