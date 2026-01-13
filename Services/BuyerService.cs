using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for Buyer operations with caching support
/// </summary>
public class BuyerService
{
    private readonly BuyerRepository _repository;
    private readonly CachedDataService<Buyer> _cachedDataService;
    private readonly SsrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BuyerService> _logger;

    private const string CacheKeyPrefix = "Buyer";
    private const string AllBuyersCacheKey = $"{CacheKeyPrefix}_All";

    public BuyerService(
        BuyerRepository repository,
        CachedDataService<Buyer> cachedDataService,
        SsrDbContext context,
        IMemoryCache cache,
        ILogger<BuyerService> logger)
    {
        _repository = repository;
        _cachedDataService = cachedDataService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated buyers with filtering and sorting
    /// </summary>
    public async Task<PagedResult<Buyer>> GetBuyersPagedAsync(
        int page,
        int pageSize,
        List<SortDefinition>? sortDefinitions = null,
        List<FilterDefinition>? filterDefinitions = null)
    {
        try
        {
            var query = await _repository.GetBuyersAsync();

            // Apply column filters
            query = ApplyColumnFilters(query, filterDefinitions);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, sortDefinitions);

            // Apply pagination
            var items = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Buyer>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated buyers");
            throw;
        }
    }

    /// <summary>
    /// Get buyer by ID with caching
    /// </summary>
    public async Task<Buyer?> GetByIdAsync(int buyerId)
    {
        return await _cachedDataService.GetByIdAsync(buyerId);
    }

    /// <summary>
    /// Get all buyers with optional filter (cached)
    /// </summary>
    public async Task<List<Buyer>> GetAllAsync(Expression<Func<Buyer, bool>>? filter = null)
    {
        return await _cachedDataService.GetAllAsync(filter);
    }

    /// <summary>
    /// Get the default buyer
    /// </summary>
    public async Task<Buyer?> GetDefaultBuyerAsync()
    {
        var query = await _repository.GetBuyersAsync();
        return await query.FirstOrDefaultAsync(b => b.DefaultBuyer == true);
    }

    /// <summary>
    /// Create a new buyer
    /// </summary>
    public async Task<int> CreateAsync(Buyer buyer)
    {
        try
        {
            // If this buyer is set as default, clear other defaults
            if (buyer.DefaultBuyer == true)
            {
                await ClearDefaultBuyerAsync();
            }

            await _repository.AddAsync(buyer);
            await _repository.SaveChangesAsync();

            InvalidateCache();

            _logger.LogInformation("Created buyer {BuyerId}", buyer.BuyerID);
            return buyer.BuyerID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating buyer");
            throw;
        }
    }

    /// <summary>
    /// Update an existing buyer
    /// </summary>
    public async Task UpdateAsync(Buyer buyer)
    {
        try
        {
            // If this buyer is set as default, clear other defaults
            if (buyer.DefaultBuyer == true)
            {
                await ClearDefaultBuyerAsync(buyer.BuyerID);
            }

            _repository.Update(buyer);
            await _repository.SaveChangesAsync();

            InvalidateCache(buyer.BuyerID);

            _logger.LogInformation("Updated buyer {BuyerId}", buyer.BuyerID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating buyer {BuyerId}", buyer.BuyerID);
            throw;
        }
    }

    /// <summary>
    /// Delete a buyer
    /// </summary>
    public async Task DeleteAsync(int buyerId)
    {
        try
        {
            var buyer = await _repository.GetByIdAsync(buyerId);
            if (buyer != null)
            {
                _repository.Delete(buyer);
                await _repository.SaveChangesAsync();

                InvalidateCache(buyerId);

                _logger.LogInformation("Deleted buyer {BuyerId}", buyerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting buyer {BuyerId}", buyerId);
            throw;
        }
    }

    /// <summary>
    /// Check if buyer name is unique
    /// </summary>
    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null)
    {
        var query = await _repository.GetBuyersAsync();
        return !await query.AnyAsync(b => b.BuyerName == name && (!excludeId.HasValue || b.BuyerID != excludeId.Value));
    }

    /// <summary>
    /// Check if buyer has associated acquisitions
    /// </summary>
    public async Task<bool> HasAssociatedAcquisitionsAsync(int buyerId)
    {
        return await _context.AcquisitionBuyers
            .AnyAsync(ab => ab.BuyerID == buyerId);
    }

    /// <summary>
    /// Set a buyer as the default buyer
    /// </summary>
    public async Task SetDefaultBuyerAsync(int buyerId)
    {
        try
        {
            // Clear existing default
            await ClearDefaultBuyerAsync(buyerId);

            // Set new default
            var buyer = await _repository.GetByIdAsync(buyerId);
            if (buyer != null)
            {
                buyer.DefaultBuyer = true;
                _repository.Update(buyer);
                await _repository.SaveChangesAsync();

                InvalidateCache();

                _logger.LogInformation("Set buyer {BuyerId} as default", buyerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default buyer {BuyerId}", buyerId);
            throw;
        }
    }

    /// <summary>
    /// Export buyers to Excel bytes
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(List<FilterDefinition>? filterDefinitions = null)
    {
        var query = await _repository.GetBuyersAsync();
        query = ApplyColumnFilters(query, filterDefinitions);

        var buyers = await query.ToListAsync();

        return await GenerateExcelBytes(buyers);
    }

    #region Private Helper Methods

    private async Task ClearDefaultBuyerAsync(int? exceptBuyerId = null)
    {
        var query = await _repository.GetBuyersAsync();
        var defaultBuyers = await query
            .Where(b => b.DefaultBuyer == true && (exceptBuyerId == null || b.BuyerID != exceptBuyerId))
            .ToListAsync();

        foreach (var buyer in defaultBuyers)
        {
            buyer.DefaultBuyer = false;
            _repository.Update(buyer);
        }

        if (defaultBuyers.Any())
        {
            await _repository.SaveChangesAsync();
        }
    }

    private IQueryable<Buyer> ApplyColumnFilters(IQueryable<Buyer> query, List<FilterDefinition>? filters)
    {
        if (filters == null || !filters.Any())
            return query;

        foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
        {
            query = ApplyFilter(query, filter);
        }

        return query;
    }

    private IQueryable<Buyer> ApplyFilter(IQueryable<Buyer> query, FilterDefinition filter)
    {
        var value = filter.Value?.ToLower() ?? string.Empty;

        return filter.Field switch
        {
            "BuyerID" when int.TryParse(filter.Value, out var id) =>
                query.Where(b => b.BuyerID == id),

            "BuyerName" =>
                query.Where(b => b.BuyerName != null && b.BuyerName.ToLower().Contains(value)),

            "DefaultBuyer" when bool.TryParse(filter.Value, out var isDefault) =>
                query.Where(b => b.DefaultBuyer == isDefault),

            "DefaultCommission" when decimal.TryParse(filter.Value, out var commission) =>
                query.Where(b => b.DefaultCommission == commission),

            "ContactName" =>
                query.Where(b => b.ContactName != null && b.ContactName.ToLower().Contains(value)),

            "ContactEmail" =>
                query.Where(b => b.ContactEmail != null && b.ContactEmail.ToLower().Contains(value)),

            "ContactPhone" =>
                query.Where(b => b.ContactPhone != null && b.ContactPhone.ToLower().Contains(value)),

            "ContactFax" =>
                query.Where(b => b.ContactFax != null && b.ContactFax.ToLower().Contains(value)),

            "City" =>
                query.Where(b => b.City != null && b.City.ToLower().Contains(value)),

            "StateCode" =>
                query.Where(b => b.StateCode != null && b.StateCode.ToLower().Contains(value)),

            "ZipCode" =>
                query.Where(b => b.ZipCode != null && b.ZipCode.ToLower().Contains(value)),

            "AddressLine1" =>
                query.Where(b => b.AddressLine1 != null && b.AddressLine1.ToLower().Contains(value)),

            _ => query
        };
    }

    private IQueryable<Buyer> ApplySorting(IQueryable<Buyer> query, List<SortDefinition>? sortDefinitions)
    {
        if (sortDefinitions == null || !sortDefinitions.Any())
        {
            // Default sort by BuyerName
            return query.OrderBy(b => b.BuyerName);
        }

        IOrderedQueryable<Buyer>? orderedQuery = null;

        foreach (var sort in sortDefinitions)
        {
            orderedQuery = ApplySort(orderedQuery ?? query.OrderBy(b => 0), sort, orderedQuery == null);
        }

        return orderedQuery ?? query;
    }

    private IOrderedQueryable<Buyer> ApplySort(IOrderedQueryable<Buyer> query, SortDefinition sort, bool isFirst)
    {
        return sort.SortBy switch
        {
            "BuyerID" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.BuyerID) : query.OrderBy(b => b.BuyerID))
                : (sort.Descending ? query.ThenByDescending(b => b.BuyerID) : query.ThenBy(b => b.BuyerID)),

            "BuyerName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.BuyerName) : query.OrderBy(b => b.BuyerName))
                : (sort.Descending ? query.ThenByDescending(b => b.BuyerName) : query.ThenBy(b => b.BuyerName)),

            "DefaultBuyer" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.DefaultBuyer) : query.OrderBy(b => b.DefaultBuyer))
                : (sort.Descending ? query.ThenByDescending(b => b.DefaultBuyer) : query.ThenBy(b => b.DefaultBuyer)),

            "DefaultCommission" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.DefaultCommission) : query.OrderBy(b => b.DefaultCommission))
                : (sort.Descending ? query.ThenByDescending(b => b.DefaultCommission) : query.ThenBy(b => b.DefaultCommission)),

            "ContactName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.ContactName) : query.OrderBy(b => b.ContactName))
                : (sort.Descending ? query.ThenByDescending(b => b.ContactName) : query.ThenBy(b => b.ContactName)),

            "ContactEmail" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.ContactEmail) : query.OrderBy(b => b.ContactEmail))
                : (sort.Descending ? query.ThenByDescending(b => b.ContactEmail) : query.ThenBy(b => b.ContactEmail)),

            "City" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.City) : query.OrderBy(b => b.City))
                : (sort.Descending ? query.ThenByDescending(b => b.City) : query.ThenBy(b => b.City)),

            "StateCode" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.StateCode) : query.OrderBy(b => b.StateCode))
                : (sort.Descending ? query.ThenByDescending(b => b.StateCode) : query.ThenBy(b => b.StateCode)),

            "ZipCode" => isFirst
                ? (sort.Descending ? query.OrderByDescending(b => b.ZipCode) : query.OrderBy(b => b.ZipCode))
                : (sort.Descending ? query.ThenByDescending(b => b.ZipCode) : query.ThenBy(b => b.ZipCode)),

            _ => query
        };
    }

    private void InvalidateCache(int? buyerId = null)
    {
        if (buyerId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}_{buyerId}");
        }

        _cache.Remove(AllBuyersCacheKey);
        _cachedDataService.InvalidateCache();

        _logger.LogInformation("Cache invalidated for Buyers");
    }

    private async Task<byte[]> GenerateExcelBytes(List<Buyer> buyers)
    {
        // Placeholder - implement with ClosedXML or EPPlus
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    #endregion
}