using ZaloOA.Domain.Entities;

namespace ZaloOA.Application.Interfaces;

public interface IZaloMessageRepository : IRepository<ZaloMessage>
{
    Task<IEnumerable<ZaloMessage>> GetByConversationIdAsync(Guid conversationId, int offset, int limit);
    Task<int> CountByConversationIdAsync(Guid conversationId);
    Task<ZaloMessage?> GetByZaloMessageIdAsync(string zaloMessageId);
}
