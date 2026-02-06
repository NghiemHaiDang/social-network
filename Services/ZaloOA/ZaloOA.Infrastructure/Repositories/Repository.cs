using System.Linq.Expressions;
using MongoDB.Driver;
using ZaloOA.Application.Interfaces;
using ZaloOA.Domain.Common;
using ZaloOA.Infrastructure.Data;

namespace ZaloOA.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;

    public Repository(MongoDbContext context)
    {
        _collection = context.GetCollection<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).FirstOrDefaultAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
    }

    public virtual void Update(T entity)
    {
        _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity).GetAwaiter().GetResult();
    }

    public virtual void Remove(T entity)
    {
        _collection.DeleteOneAsync(x => x.Id == entity.Id).GetAwaiter().GetResult();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
            return (int)await _collection.CountDocumentsAsync(_ => true);

        return (int)await _collection.CountDocumentsAsync(predicate);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).AnyAsync();
    }
}
