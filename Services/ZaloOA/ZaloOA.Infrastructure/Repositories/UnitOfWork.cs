using Microsoft.EntityFrameworkCore.Storage;
using ZaloOA.Application.Interfaces;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ZaloOADbContext _context;
    private IDbContextTransaction? _transaction;

    public IZaloOAAccountRepository ZaloOAAccounts { get; }
    public IZaloUserRepository ZaloUsers { get; }
    public IZaloConversationRepository ZaloConversations { get; }
    public IZaloMessageRepository ZaloMessages { get; }

    public UnitOfWork(
        ZaloOADbContext context,
        IZaloOAAccountRepository zaloOAAccountRepository,
        IZaloUserRepository zaloUserRepository,
        IZaloConversationRepository zaloConversationRepository,
        IZaloMessageRepository zaloMessageRepository)
    {
        _context = context;
        ZaloOAAccounts = zaloOAAccountRepository;
        ZaloUsers = zaloUserRepository;
        ZaloConversations = zaloConversationRepository;
        ZaloMessages = zaloMessageRepository;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
