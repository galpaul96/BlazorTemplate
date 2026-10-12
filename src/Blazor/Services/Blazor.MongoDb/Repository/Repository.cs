using Blazor.Domain.MongoDb;
using MongoDB.Driver;

namespace Blazor.MongoDb.Repository;

public class Repository<T> : IRepository<T> where T : MongoBaseEntity
{
    private readonly IMongoCollection<T> _collection;

    public Repository(IMongoDatabase database)
    {
        _collection = database.GetCollection<T>(GetCollectionName());
    }

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        entity.CreatedDate = DateTime.UtcNow;

        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);

        return entity;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(entity => entity.Id == id && !entity.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(entity => !entity.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedDate = DateTime.UtcNow;

        var result = await _collection.ReplaceOneAsync(
            existing => existing.Id == entity.Id && !existing.IsDeleted,
            entity,
            cancellationToken: cancellationToken);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var update = Builders<T>.Update
            .Set(entity => entity.IsDeleted, true)
            .Set(entity => entity.DeletedDate, now)
            .Set(entity => entity.UpdatedDate, now);

        var result = await _collection.UpdateOneAsync(
            entity => entity.Id == id && !entity.IsDeleted,
            update,
            cancellationToken: cancellationToken);

        return result.MatchedCount > 0;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _collection
            .Find(entity => entity.Id == id && !entity.IsDeleted)
            .AnyAsync(cancellationToken);
    }

    private static string GetCollectionName()
    {
        var entityName = typeof(T).Name;

        // Derive the default collection name from the entity type. For example,
        // Student maps to "students" and Meeting maps to "meetings".
        return entityName[..1].ToLowerInvariant() + entityName[1..] + "s";
    }
}
