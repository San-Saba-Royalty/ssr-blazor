using Microsoft.Extensions.Caching.Memory;

namespace SSRBlazor.Services;

public class SessionStateService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionStateService> _logger;
  
    public SessionStateService(IMemoryCache cache, ILogger<SessionStateService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
  
    public void SetViewForUser(string userId, int viewId)
    {
        var key = $"User_{userId}_View";
        _cache.Set(key, viewId, TimeSpan.FromDays(30));
    }
  
    public int? GetViewForUser(string userId)
    {
        var key = $"User_{userId}_View";
        return _cache.TryGetValue(key, out int viewId) ? viewId : null;
    }

    public void SetLastAcquisitionId(string userId, int acquisitionId)
    {
        var key = $"User_{userId}_LastAcquisition";
        _cache.Set(key, acquisitionId, TimeSpan.FromDays(30));
    }

    public int? GetLastAcquisitionId(string userId)
    {
        var key = $"User_{userId}_LastAcquisition";
        return _cache.TryGetValue(key, out int acquisitionId) ? acquisitionId : null;
    }
}
