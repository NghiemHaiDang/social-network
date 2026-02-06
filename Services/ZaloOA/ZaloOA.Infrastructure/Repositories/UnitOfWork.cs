using MongoDB.Driver;
using ZaloOA.Application.Interfaces;

namespace ZaloOA.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IMongoClient _client;
    private IClientSessionHandle? _session;

    public IZaloOAAccountRepository ZaloOAAccounts { get; }
    public IZaloUserRepository ZaloUsers { get; }
    public IZaloConversationRepository ZaloConversations { get; }
    public IZaloMessageRepository ZaloMessages { get; }

    public UnitOfWork(
        IMongoClient client,
        IZaloOAAccountRepository zaloOAAccountRepository,
        IZaloUserRepository zaloUserRepository,
        IZaloConversationRepository zaloConversationRepository,
        IZaloMessageRepository zaloMessageRepository)
    {
        _client = client;
        ZaloOAAccounts = zaloOAAccountRepository;
        ZaloUsers = zaloUserRepository;
        ZaloConversations = zaloConversationRepository;
        ZaloMessages = zaloMessageRepository;
    }

    public Task<int> SaveChangesAsync()
    {
        // MongoDB writes are immediate per operation, no deferred save.
        // This is a no-op for compatibility with the IUnitOfWork interface.
        return Task.FromResult(0);
    }

    public async Task BeginTransactionAsync()
    {
        _session = await _client.StartSessionAsync();
        _session.StartTransaction();
    }

    public async Task CommitTransactionAsync()
    {
        if (_session != null)
        {
            await _session.CommitTransactionAsync();
            _session.Dispose();
            _session = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_session != null)
        {
            await _session.AbortTransactionAsync();
            _session.Dispose();
            _session = null;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
