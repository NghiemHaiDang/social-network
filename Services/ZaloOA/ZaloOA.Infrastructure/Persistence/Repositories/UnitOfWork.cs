using Microsoft.EntityFrameworkCore.Storage;
using ZaloOA.Application.Common.Interfaces;

namespace ZaloOA.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ZaloOADbContext _context;
    private IDbContextTransaction? _transaction;

    public IZaloOAAccountRepository ZaloOAAccounts { get; }

    public UnitOfWork(ZaloOADbContext context, IZaloOAAccountRepository zaloOAAccountRepository)
    {
        _context = context;
        ZaloOAAccounts = zaloOAAccountRepository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
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
