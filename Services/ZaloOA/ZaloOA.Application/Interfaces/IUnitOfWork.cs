namespace ZaloOA.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IZaloOAAccountRepository ZaloOAAccounts { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
