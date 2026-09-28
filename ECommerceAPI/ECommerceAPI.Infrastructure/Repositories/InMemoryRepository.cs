using ECommerceAPI.Domain.Common;
using ECommerceAPI.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ECommerceAPI.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory generic repository using ConcurrentDictionary
/// </summary>
public class InMemoryRepository<T> : IRepository<T> where T : BaseEntity
{
    internal readonly ConcurrentDictionary<Guid, T> _store = new();

    public Task<T?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        return Task.FromResult(_store.Values.AsEnumerable());
    }

    public Task<T> AddAsync(T entity)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        _store[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<T> UpdateAsync(T entity)
    {
        _store[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return Task.FromResult(_store.TryRemove(id, out _));
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        return Task.FromResult(_store.ContainsKey(id));
    }
}
