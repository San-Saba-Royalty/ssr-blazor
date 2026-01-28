namespace SSRBlazor.Services;

/// <summary>
/// Hosted service that warms the cache on application startup
/// </summary>
public class CacheWarmingService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CacheWarmingService> _logger;

    public CacheWarmingService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CacheWarmingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CacheWarmingService starting");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var viewCacheService = scope.ServiceProvider.GetRequiredService<ViewCacheService>();

            await viewCacheService.WarmCacheAsync();

            _logger.LogInformation("CacheWarmingService completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warming");
            // Don't throw - allow application to start even if cache warming fails
        }

        return;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CacheWarmingService stopping");
        return Task.CompletedTask;
    }
}
