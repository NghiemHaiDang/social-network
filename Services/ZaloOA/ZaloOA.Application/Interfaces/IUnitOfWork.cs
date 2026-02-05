namespace ZaloOA.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IZaloOAAccountRepository ZaloOAAccounts { get; }
    IZaloUserRepository ZaloUsers { get; }
    IZaloConversationRepository ZaloConversations { get; }
    IZaloMessageRepository ZaloMessages { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
