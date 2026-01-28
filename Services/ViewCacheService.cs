using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.Entities;
using SSRBlazor.Models;
using SSRBusiness.BusinessClasses;

namespace SSRBlazor.Services;

/// <summary>
/// Singleton service for caching View-related data with high performance
/// </summary>
public class ViewCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ViewCacheService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    // Cache keys
    private const string VIEW_BY_MODULE_KEY = "Views_Module_{0}";
    private const string DISPLAYFIELDS_BY_MODULE_KEY = "DisplayFields_Module_{0}";
    private const string USER_PREFERENCES_KEY = "UserPreferences_{0}";
    private const string VIEW_CONFIG_KEY = "ViewConfig_{0}";

    public ViewCacheService(
        IMemoryCache cache,
        ILogger<ViewCacheService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _cache = cache;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #region Cache Warming (Startup)

    /// <summary>
    /// Warm cache with Views and DisplayFields on application startup
    /// </summary>
    public async Task WarmCacheAsync()
    {
        _logger.LogInformation("Starting cache warming for Views and DisplayFields");

        using var scope = _serviceScopeFactory.CreateScope();
        var viewRepository = scope.ServiceProvider.GetRequiredService<ViewRepository>();
        var displayFieldRepository = scope.ServiceProvider.GetRequiredService<DisplayFieldRepository>();

        // Load all modules
        var modules = new[] { "Acquisition", "LetterAgreement", "Document", "Operator", "Referrer", "Filter" };

        foreach (var module in modules)
        {
            try
            {
                // Cache Views for module
                var views = await viewRepository.GetViewsAsync(module).ToListAsync();
                var viewsKey = string.Format(VIEW_BY_MODULE_KEY, module);
                _cache.Set(viewsKey, views, TimeSpan.FromHours(24));
                _logger.LogInformation("Cached {Count} views for module {Module}", views.Count, module);

                // Cache DisplayFields for module
                var displayFields = await displayFieldRepository.GetDisplayFieldsAsync(module);
                var fieldsKey = string.Format(DISPLAYFIELDS_BY_MODULE_KEY, module);
                _cache.Set(fieldsKey, displayFields, TimeSpan.FromHours(24));
                _logger.LogInformation("Cached {Count} display fields for module {Module}", displayFields.Count, module);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming cache for module {Module}", module);
            }
        }

        _logger.LogInformation("Cache warming completed");
    }

    #endregion

    #region Views

    /// <summary>
    /// Get all views for a module (from cache)
    /// </summary>
    public async Task<List<View>> GetViewsForModuleAsync(string module)
    {
        var key = string.Format(VIEW_BY_MODULE_KEY, module);

        if (!_cache.TryGetValue(key, out List<View>? views) || views == null)
        {
            _logger.LogWarning("Views cache miss for module {Module}, loading from database", module);

            using var scope = _serviceScopeFactory.CreateScope();
            var viewRepository = scope.ServiceProvider.GetRequiredService<ViewRepository>();

            views = await viewRepository.GetViewsAsync(module).ToListAsync();
            _cache.Set(key, views, TimeSpan.FromHours(24));
        }

        return views;
    }

    /// <summary>
    /// Invalidate views cache for a module (call after creating/updating/deleting views)
    /// </summary>
    public void InvalidateViewsCache(string module)
    {
        var key = string.Format(VIEW_BY_MODULE_KEY, module);
        _cache.Remove(key);
        _logger.LogInformation("Invalidated views cache for module {Module}", module);
    }

    #endregion

    #region DisplayFields

    /// <summary>
    /// Get all display fields for a module (from cache)
    /// </summary>
    public async Task<List<DisplayField>> GetDisplayFieldsForModuleAsync(string module)
    {
        var key = string.Format(DISPLAYFIELDS_BY_MODULE_KEY, module);

        if (!_cache.TryGetValue(key, out List<DisplayField>? fields) || fields == null)
        {
            _logger.LogWarning("DisplayFields cache miss for module {Module}, loading from database", module);

            using var scope = _serviceScopeFactory.CreateScope();
            var displayFieldRepository = scope.ServiceProvider.GetRequiredService<DisplayFieldRepository>();

            fields = await displayFieldRepository.GetDisplayFieldsAsync(module);
            _cache.Set(key, fields, TimeSpan.FromHours(24));
        }

        return fields;
    }

    #endregion

    #region User Preferences

    /// <summary>
    /// Get user's view configuration for a specific page
    /// </summary>
    public async Task<ViewConfiguration?> GetUserViewForPageAsync(string userId, string pageName, string module)
    {
        // Check cache for user's preference
        var userPrefKey = string.Format(USER_PREFERENCES_KEY, userId);

        if (!_cache.TryGetValue(userPrefKey, out Dictionary<string, int>? userPreferences))
        {
            // Load user's preferences into cache
            await LoadUserPreferencesAsync(userId);
            _cache.TryGetValue(userPrefKey, out userPreferences);
        }

        if (userPreferences != null && userPreferences.TryGetValue(pageName, out int viewId))
        {
            // Get the view configuration
            return await GetViewConfigurationAsync(viewId, module);
        }

        return null;
    }

    /// <summary>
    /// Load all user preferences into cache (called on user login)
    /// </summary>
    public async Task LoadUserPreferencesAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        if (!int.TryParse(userId, out int userIdInt))
            return;

        _logger.LogInformation("Loading preferences for user {UserId}", userId);

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SSRBusiness.Data.SsrDbContext>();

        // Load all user page preferences
        var preferences = await context.UserPagePreferences
            .Where(p => p.UserID == userIdInt)
            .Select(p => new { p.PageName, p.ViewID })
            .ToListAsync();

        var userPrefDict = preferences.ToDictionary(p => p.PageName, p => p.ViewID);

        var key = string.Format(USER_PREFERENCES_KEY, userId);
        _cache.Set(key, userPrefDict, TimeSpan.FromHours(24));

        _logger.LogInformation("Loaded {Count} preferences for user {UserId}", preferences.Count, userId);
    }

    /// <summary>
    /// Save user view preference (persists to DB and updates cache)
    /// </summary>
    public async Task SaveUserViewPreferenceAsync(string userId, string pageName, ViewConfiguration viewConfig)
    {
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            return;

        using var scope = _serviceScopeFactory.CreateScope();
        var viewService = scope.ServiceProvider.GetRequiredService<ViewService>();

        // Get or create user's view for this page
        var existingPref = await GetUserViewForPageAsync(userId, pageName, viewConfig.Module);

        ViewConfiguration savedView;
        if (existingPref != null && existingPref.ViewID > 0)
        {
            // Update existing view
            viewConfig.ViewID = existingPref.ViewID;
            var updateResult = await viewService.UpdateAsync(viewConfig);
            if (!updateResult.Success || updateResult.View == null)
            {
                _logger.LogError("Failed to update view: {Error}", updateResult.Error);
                throw new Exception(updateResult.Error ?? "Failed to update view");
            }
            savedView = viewConfig;
        }
        else
        {
            // Create new view for user
            viewConfig.ViewName = $"User {userId} - {pageName}";
            var createResult = await viewService.CreateAsync(viewConfig);
            if (!createResult.Success || createResult.View == null)
            {
                _logger.LogError("Failed to create view: {Error}", createResult.Error);
                throw new Exception(createResult.Error ?? "Failed to create view");
            }
            savedView = viewConfig;
            savedView.ViewID = createResult.View.ViewID;

            // Save user preference
            var viewRepository = scope.ServiceProvider.GetRequiredService<ViewRepository>();
            await viewRepository.SaveUserPagePreferenceAsync(new UserPagePreference
            {
                UserID = userIdInt,
                PageName = pageName,
                ViewID = savedView.ViewID
            });
        }

        // Update cache
        var userPrefKey = string.Format(USER_PREFERENCES_KEY, userId);
        if (_cache.TryGetValue(userPrefKey, out Dictionary<string, int>? userPreferences))
        {
            userPreferences![pageName] = savedView.ViewID;
        }
        else
        {
            userPreferences = new Dictionary<string, int> { { pageName, savedView.ViewID } };
        }
        _cache.Set(userPrefKey, userPreferences, TimeSpan.FromHours(24));

        // Invalidate view config cache for this view
        var viewConfigKey = string.Format(VIEW_CONFIG_KEY, savedView.ViewID);
        _cache.Remove(viewConfigKey);

        _logger.LogInformation("Saved view preference for user {UserId}, page {PageName}, viewId {ViewId}",
            userId, pageName, savedView.ViewID);
    }

    /// <summary>
    /// Invalidate user's cache (call on logout or when user preferences should be refreshed)
    /// </summary>
    public void InvalidateUserCacheAsync(string userId)
    {
        var key = string.Format(USER_PREFERENCES_KEY, userId);
        _cache.Remove(key);
        _logger.LogInformation("Invalidated cache for user {UserId}", userId);
    }

    #endregion

    #region View Configuration

    /// <summary>
    /// Get full view configuration with fields (cached)
    /// </summary>
    private async Task<ViewConfiguration?> GetViewConfigurationAsync(int viewId, string module)
    {
        var key = string.Format(VIEW_CONFIG_KEY, viewId);

        if (!_cache.TryGetValue(key, out ViewConfiguration? viewConfig) || viewConfig == null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var viewService = scope.ServiceProvider.GetRequiredService<ViewService>();

            viewConfig = await viewService.GetViewConfigurationAsync(viewId);
            if (viewConfig != null)
            {
                _cache.Set(key, viewConfig, TimeSpan.FromHours(1));
            }
        }

        return viewConfig;
    }

    #endregion
}
