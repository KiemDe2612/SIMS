using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Infrastructure.Repositories;

/// <summary>
/// Decorator Pattern: Bọc quanh IGenericRepository&lt;Student&gt; để bổ sung tính năng Caching cho GetById.
/// </summary>
public class CachingStudentRepository : IGenericRepository<Student>
{
    private readonly IGenericRepository<Student> _innerRepository;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    public CachingStudentRepository(IGenericRepository<Student> innerRepository, IMemoryCache cache)
    {
        _innerRepository = innerRepository;
        _cache = cache;
    }

    public IGenericRepository<Student> InnerRepository => _innerRepository;

    private static string GetCacheKey(Guid id) => $"student_{id}";

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        string key = GetCacheKey(id);
        if (_cache.TryGetValue(key, out Student? cachedStudent))
        {
            return cachedStudent;
        }

        var student = await _innerRepository.GetByIdAsync(id);
        if (student != null)
        {
            _cache.Set(key, student, DefaultCacheDuration);
        }

        return student;
    }

    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _innerRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Student>> FindAsync(Expression<Func<Student, bool>> expression)
    {
        return await _innerRepository.FindAsync(expression);
    }

    public async Task<(IEnumerable<Student> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Student, bool>>? predicate = null)
    {
        return await _innerRepository.GetPagedAsync(pageIndex, pageSize, predicate);
    }

    public async Task AddAsync(Student entity)
    {
        await _innerRepository.AddAsync(entity);
        _cache.Set(GetCacheKey(entity.Id), entity, DefaultCacheDuration);
    }

    public async Task AddRangeAsync(IEnumerable<Student> entities)
    {
        await _innerRepository.AddRangeAsync(entities);
        foreach (var entity in entities)
        {
            _cache.Set(GetCacheKey(entity.Id), entity, DefaultCacheDuration);
        }
    }

    public void Update(Student entity)
    {
        _innerRepository.Update(entity);
        _cache.Remove(GetCacheKey(entity.Id));
    }

    public void Remove(Student entity)
    {
        _innerRepository.Remove(entity);
        _cache.Remove(GetCacheKey(entity.Id));
    }

    public void RemoveRange(IEnumerable<Student> entities)
    {
        _innerRepository.RemoveRange(entities);
        foreach (var entity in entities)
        {
            _cache.Remove(GetCacheKey(entity.Id));
        }
    }
}

