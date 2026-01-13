using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.BusinessFramework;

namespace SSRBlazor.Services;

public class CachedDataService<T> where T : class
{
    private readonly Repository<T> _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedDataService<T>> _logger;
    
    public CachedDataService(
        Repository<T> repository, 
        IMemoryCache cache,
        ILogger<CachedDataService<T>> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<List<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        string? cacheKey = null)
    {
        cacheKey ??= $"{typeof(T).Name}_All_{filter?.ToString() ?? "NoFilter"}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Cache miss for {CacheKey}, fetching from database", cacheKey);
            
            var query = _repository.Query();
            
            if (filter != null)
            {
                query = query.Where(filter);
            }
            
            return await query.ToListAsync();
        }) ?? new List<T>();
    }
    
    public async Task<T?> GetByIdAsync(int id)
    {
        var cacheKey = $"{typeof(T).Name}_{id}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            
            return await _repository.GetByIdAsync(id);
        });
    }
    
    public void InvalidateCache(string? specificKey = null)
    {
        if (specificKey != null)
        {
            _cache.Remove(specificKey);
        }
        else
        {
            // Remove all cached entries for this type
            // Note: IMemoryCache doesn't support wildcard removal natively
            // Consider using a key registry or distributed cache for this
            _logger.LogWarning("Full cache invalidation not implemented for IMemoryCache");
        }
    }
}
