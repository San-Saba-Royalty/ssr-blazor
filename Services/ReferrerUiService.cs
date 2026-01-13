using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for Referrer operations with caching support
/// </summary>
public class ReferrerUiService
{
    private readonly ReferrerRepository _repository;
    private readonly SSRBusiness.BusinessClasses.ReferrerService _businessService;
    private readonly CachedDataService<Referrer> _cachedDataService;
    private readonly SsrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReferrerUiService> _logger;

    private const string CacheKeyPrefix = "Referrer";
    private const string AllReferrersCacheKey = $"{CacheKeyPrefix}_All";

    public ReferrerUiService(
        ReferrerRepository repository,
        SSRBusiness.BusinessClasses.ReferrerService businessService,
        CachedDataService<Referrer> cachedDataService,
        SsrDbContext context,
        IMemoryCache cache,
        ILogger<ReferrerUiService> logger)
    {
        _repository = repository;
        _businessService = businessService;
        _cachedDataService = cachedDataService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated referrers with filtering and sorting
    /// </summary>
    public async Task<PagedResult<Referrer>> GetReferrersPagedAsync(
        int page,
        int pageSize,
        List<SortDefinition>? sortDefinitions = null,
        List<FilterDefinition>? filterDefinitions = null)
    {
        try
        {
            var query = _repository.GetReferrersQuery();

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

            return new PagedResult<Referrer>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated referrers");
            throw;
        }
    }

    /// <summary>
    /// Get referrer by ID with caching
    /// </summary>
    public async Task<Referrer?> GetByIdAsync(int referrerId)
    {
        return await _cachedDataService.GetByIdAsync(referrerId);
    }

    /// <summary>
    /// Get all referrers with optional filter (cached)
    /// </summary>
    public async Task<List<Referrer>> GetAllAsync(Expression<Func<Referrer, bool>>? filter = null)
    {
        return await _cachedDataService.GetAllAsync(filter);
    }

    /// <summary>
    /// Create a new referrer
    /// </summary>
    public async Task<int> CreateAsync(Referrer referrer)
    {
        try
        {
            await _businessService.SaveReferrerAsync(referrer);
            
            InvalidateCache();

            _logger.LogInformation("Created referrer {ReferrerId}", referrer.ReferrerID);
            return referrer.ReferrerID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating referrer");
            throw;
        }
    }

    /// <summary>
    /// Update an existing referrer
    /// </summary>
    public async Task UpdateAsync(Referrer referrer)
    {
        try
        {
            await _businessService.SaveReferrerAsync(referrer);

            InvalidateCache(referrer.ReferrerID);

            _logger.LogInformation("Updated referrer {ReferrerId}", referrer.ReferrerID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating referrer {ReferrerId}", referrer.ReferrerID);
            throw;
        }
    }

    /// <summary>
    /// Delete a referrer
    /// </summary>
    public async Task DeleteAsync(int referrerId)
    {
        try
        {
            await _businessService.DeleteReferrerAsync(referrerId);
            
            InvalidateCache(referrerId);

            _logger.LogInformation("Deleted referrer {ReferrerId}", referrerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting referrer {ReferrerId}", referrerId);
            throw;
        }
    }

    /// <summary>
    /// Check if referrer has associated acquisitions
    /// </summary>
    public async Task<bool> HasAssociatedAcquisitionsAsync(int referrerId)
    {
        // Check if this referrer is linked to any acquisitions through AcquisitionReferrers table
        return await _context.AcquisitionReferrers
            .AnyAsync(ar => ar.ReferrerID == referrerId);
    }

    /// <summary>
    /// Get forms for a referrer
    /// </summary>
    public async Task<List<ReferrerForm>> GetReferrerFormsAsync(int referrerId)
    {
        return await _businessService.GetReferrerFormsAsync(referrerId);
    }

    /// <summary>
    /// Upload a new form for a referrer
    /// </summary>
    public async Task UploadReferrerFormAsync(ReferrerForm form, Stream fileStream, string fileName)
    {
        try 
        {
            await _businessService.UploadReferrerFormAsync(form, fileStream, fileName);
            _logger.LogInformation("Uploaded form {FileName} for referrer {ReferrerId}", fileName, form.ReferrerID);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error uploading form for referrer {ReferrerId}", form.ReferrerID);
             throw;
        }
    }

    /// <summary>
    /// Export referrers to Excel bytes
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(List<FilterDefinition>? filterDefinitions = null)
    {
        var query = _repository.GetReferrersQuery();
        query = ApplyColumnFilters(query, filterDefinitions);

        var referrers = await query.ToListAsync();

        return await GenerateExcelBytes(referrers);
    }

    #region Private Helper Methods

    private IQueryable<Referrer> ApplyColumnFilters(IQueryable<Referrer> query, List<FilterDefinition>? filters)
    {
        if (filters == null || !filters.Any())
            return query;

        foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
        {
            query = ApplyFilter(query, filter);
        }

        return query;
    }

    private IQueryable<Referrer> ApplyFilter(IQueryable<Referrer> query, FilterDefinition filter)
    {
        var value = filter.Value?.ToLower() ?? string.Empty;

        return filter.Field switch
        {
            "ReferrerID" when int.TryParse(filter.Value, out var id) =>
                query.Where(r => r.ReferrerID == id),

            "ReferrerName" =>
                query.Where(r => r.ReferrerName != null && r.ReferrerName.ToLower().Contains(value)),

            "ReferrerTaxID" =>
                query.Where(r => r.ReferrerTaxID != null && r.ReferrerTaxID.ToLower().Contains(value)),

            "ContactName" =>
                query.Where(r => r.ContactName != null && r.ContactName.ToLower().Contains(value)),

            "ContactEmail" =>
                query.Where(r => r.ContactEmail != null && r.ContactEmail.ToLower().Contains(value)),

            "ContactPhone" =>
                query.Where(r => r.ContactPhone != null && r.ContactPhone.ToLower().Contains(value)),

            "City" =>
                query.Where(r => r.City != null && r.City.ToLower().Contains(value)),

            "StateCode" =>
                query.Where(r => r.StateCode != null && r.StateCode.ToLower().Contains(value)),

            "ZipCode" =>
                query.Where(r => r.ZipCode != null && r.ZipCode.ToLower().Contains(value)),

            "AddressLine1" =>
                query.Where(r => r.AddressLine1 != null && r.AddressLine1.ToLower().Contains(value)),

            _ => query
        };
    }

    private IQueryable<Referrer> ApplySorting(IQueryable<Referrer> query, List<SortDefinition>? sortDefinitions)
    {
        if (sortDefinitions == null || !sortDefinitions.Any())
        {
            // Default sort by ReferrerName
            return query.OrderBy(r => r.ReferrerName);
        }

        IOrderedQueryable<Referrer>? orderedQuery = null;

        foreach (var sort in sortDefinitions)
        {
            orderedQuery = ApplySort(orderedQuery ?? query.OrderBy(r => 0), sort, orderedQuery == null);
        }

        return orderedQuery ?? query;
    }

    private IOrderedQueryable<Referrer> ApplySort(IOrderedQueryable<Referrer> query, SortDefinition sort, bool isFirst)
    {
        return sort.SortBy switch
        {
            "ReferrerID" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.ReferrerID) : query.OrderBy(r => r.ReferrerID))
                : (sort.Descending ? query.ThenByDescending(r => r.ReferrerID) : query.ThenBy(r => r.ReferrerID)),

            "ReferrerName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.ReferrerName) : query.OrderBy(r => r.ReferrerName))
                : (sort.Descending ? query.ThenByDescending(r => r.ReferrerName) : query.ThenBy(r => r.ReferrerName)),

            "ReferrerTaxID" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.ReferrerTaxID) : query.OrderBy(r => r.ReferrerTaxID))
                : (sort.Descending ? query.ThenByDescending(r => r.ReferrerTaxID) : query.ThenBy(r => r.ReferrerTaxID)),

            "ContactName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.ContactName) : query.OrderBy(r => r.ContactName))
                : (sort.Descending ? query.ThenByDescending(r => r.ContactName) : query.ThenBy(r => r.ContactName)),

            "ContactEmail" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.ContactEmail) : query.OrderBy(r => r.ContactEmail))
                : (sort.Descending ? query.ThenByDescending(r => r.ContactEmail) : query.ThenBy(r => r.ContactEmail)),

            "City" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.City) : query.OrderBy(r => r.City))
                : (sort.Descending ? query.ThenByDescending(r => r.City) : query.ThenBy(r => r.City)),

            "StateCode" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.StateCode) : query.OrderBy(r => r.StateCode))
                : (sort.Descending ? query.ThenByDescending(r => r.StateCode) : query.ThenBy(r => r.StateCode)),

            "ZipCode" => isFirst
                ? (sort.Descending ? query.OrderByDescending(r => r.ZipCode) : query.OrderBy(r => r.ZipCode))
                : (sort.Descending ? query.ThenByDescending(r => r.ZipCode) : query.ThenBy(r => r.ZipCode)),

            _ => query
        };
    }

    private void InvalidateCache(int? referrerId = null)
    {
        if (referrerId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}_{referrerId}");
        }

        _cache.Remove(AllReferrersCacheKey);
        _cachedDataService.InvalidateCache();

        _logger.LogInformation("Cache invalidated for Referrers");
    }

    private async Task<byte[]> GenerateExcelBytes(List<Referrer> referrers)
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Referrers");
            
            // Headers
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Name";
            worksheet.Cell(1, 3).Value = "Tax ID";
            worksheet.Cell(1, 4).Value = "Type";
            worksheet.Cell(1, 5).Value = "Contact Name";
            worksheet.Cell(1, 6).Value = "Email";
            worksheet.Cell(1, 7).Value = "Phone";
            worksheet.Cell(1, 8).Value = "Address";
            worksheet.Cell(1, 9).Value = "City";
            worksheet.Cell(1, 10).Value = "State";
            worksheet.Cell(1, 11).Value = "Zip";
            
            // Style Headers
            var headerRange = worksheet.Range(1, 1, 1, 11);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            // Data
            for (int i = 0; i < referrers.Count; i++)
            {
                var r = referrers[i];
                int row = i + 2;
                worksheet.Cell(row, 1).Value = r.ReferrerID;
                worksheet.Cell(row, 2).Value = r.ReferrerName;
                worksheet.Cell(row, 3).Value = r.ReferrerTaxID;
                worksheet.Cell(row, 4).Value = r.ReferrerTypeCode;
                worksheet.Cell(row, 5).Value = r.ContactName;
                worksheet.Cell(row, 6).Value = r.ContactEmail;
                worksheet.Cell(row, 7).Value = r.ContactPhone;
                worksheet.Cell(row, 8).Value = r.AddressLine1;
                worksheet.Cell(row, 9).Value = r.City;
                worksheet.Cell(row, 10).Value = r.StateCode;
                worksheet.Cell(row, 11).Value = r.ZipCode;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return await Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel file");
            return Array.Empty<byte>();
        }
    }

    #endregion
}