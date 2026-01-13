using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;
using ClosedXML.Excel;

namespace SSRBlazor.Services;

/// <summary>
/// Service for County business logic
/// </summary>
public class CountyService
{
    private readonly CountyRepository _repository;
    private readonly ILogger<CountyService> _logger;

    public CountyService(
        CountyRepository repository,
        ILogger<CountyService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    #region Read Operations

    /// <summary>
    /// Get counties with server-side paging, filtering, and sorting
    /// </summary>
    public async Task<GridData<County>> GetCountiesPagedAsync(
        GridState<County> state,
        Dictionary<string, string>? columnFilters = null)
    {
        var query = _repository.GetCountiesAsync();

        // Apply column filters
        if (columnFilters != null && columnFilters.Any())
        {
            query = ApplyColumnFilters(query, columnFilters);
        }

        // Apply MudBlazor filters
        if (state.FilterDefinitions?.Any() == true)
        {
            foreach (var filter in state.FilterDefinitions)
            {
                query = ApplyFilter(query, filter);
            }
        }

        // Get total count before paging
        var totalItems = await query.CountAsync();

        // Apply sorting
        if (state.SortDefinitions?.Any() == true)
        {
            query = ApplySorting(query, state.SortDefinitions);
        }
        else
        {
            query = query.OrderBy(c => c.CountyName);
        }

        // Apply paging
        var items = await query
            .Skip(state.Page * state.PageSize)
            .Take(state.PageSize)
            .ToListAsync();

        return new GridData<County>
        {
            Items = items,
            TotalItems = totalItems
        };
    }

    /// <summary>
    /// Get county by ID
    /// </summary>
    public async Task<County?> GetByIdAsync(int countyId)
    {
        return await _repository.GetByIdAsync(countyId);
    }

    /// <summary>
    /// Get all counties (cached)
    /// </summary>
    public async Task<List<County>> GetAllAsync()
    {
        var counties = await _repository.GetCountiesAsync()
            .OrderBy(c => c.CountyName)
            .ToListAsync();
        return counties;
    }

    #endregion

    #region Write Operations

    /// <summary>
    /// Create new county
    /// </summary>
    public async Task<(bool Success, County? County, string? Error)> CreateAsync(County county)
    {
        try
        {
            // Check for duplicate name
            if (await _repository.CountyNameExistsAsync(county.CountyName!))
            {
                return (false, null, "A county with this name already exists");
            }

            var created = await _repository.AddAsync(county);
            return (true, created, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating county");
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Update existing county
    /// </summary>
    public async Task<(bool Success, County? County, string? Error)> UpdateAsync(County county)
    {
        try
        {
            // Check for duplicate name
            if (await _repository.CountyNameExistsAsync(county.CountyName!, county.CountyID))
            {
                return (false, null, "A county with this name already exists");
            }

            var updated = await _repository.UpdateAsync(county);
            return (true, updated, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating county {CountyId}", county.CountyID);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete county
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteAsync(int countyId)
    {
        try
        {
            var county = await _repository.GetByIdAsync(countyId);
            if (county == null)
            {
                return (false, "County not found");
            }

            await _repository.DeleteAsync(county);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting county {CountyId}", countyId);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Check if county name is unique
    /// </summary>
    public async Task<bool> IsNameUniqueAsync(string name, string stateCode, int? excludeId = null)
    {
        return !await _repository.CountyNameExistsAsync(name, excludeId);
    }
    
    /// <summary>
    /// Check if county has associated acquisitions
    /// </summary>
    public async Task<bool> HasAssociatedAcquisitionsAsync(int countyId)
    {
        return await _repository.HasAssociatedAcquisitionsAsync(countyId);
    }

    #endregion

    #region Export

    /// <summary>
    /// Export counties to Excel
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(Dictionary<string, string>? columnFilters = null)
    {
        var query = _repository.GetCountiesAsync();

        if (columnFilters != null && columnFilters.Any())
        {
            query = ApplyColumnFilters(query, columnFilters);
        }

        var counties = await query
            .OrderBy(c => c.CountyName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Counties");

        // Headers
        var headers = new[] { "County Name", "Contact Name", "Contact Email", "Contact Phone",
            "Contact Fax", "Address", "City", "State", "Zip Code" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        for (int row = 0; row < counties.Count; row++)
        {
            var county = counties[row];
            var address = GetFullAddress(county);

            worksheet.Cell(row + 2, 1).Value = county.CountyName;
            worksheet.Cell(row + 2, 2).Value = county.ContactName;
            worksheet.Cell(row + 2, 3).Value = county.ContactEmail;
            worksheet.Cell(row + 2, 4).Value = county.ContactPhone;
            worksheet.Cell(row + 2, 5).Value = county.ContactFax;
            worksheet.Cell(row + 2, 6).Value = address;
            worksheet.Cell(row + 2, 7).Value = county.City;
            worksheet.Cell(row + 2, 8).Value = county.StateCode;
            worksheet.Cell(row + 2, 9).Value = county.ZipCode;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Private Methods

    private static IQueryable<County> ApplyColumnFilters(
        IQueryable<County> query,
        Dictionary<string, string> filters)
    {
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Value))
                continue;

            var value = filter.Value.ToLower();

            query = filter.Key switch
            {
                "CountyName" => query.Where(c => c.CountyName != null && c.CountyName.ToLower().Contains(value)),
                "ContactName" => query.Where(c => c.ContactName != null && c.ContactName.ToLower().Contains(value)),
                "ContactEmail" => query.Where(c => c.ContactEmail != null && c.ContactEmail.ToLower().Contains(value)),
                "ContactPhone" => query.Where(c => c.ContactPhone != null && c.ContactPhone.Contains(value)),
                "ContactFax" => query.Where(c => c.ContactFax != null && c.ContactFax.Contains(value)),
                "AddressLine1" => query.Where(c => c.AddressLine1 != null && c.AddressLine1.ToLower().Contains(value)),
                "City" => query.Where(c => c.City != null && c.City.ToLower().Contains(value)),
                "StateCode" => query.Where(c => c.StateCode != null && c.StateCode.ToLower().Contains(value)),
                "ZipCode" => query.Where(c => c.ZipCode != null && c.ZipCode.Contains(value)),
                _ => query
            };
        }

        return query;
    }

    private static IQueryable<County> ApplyFilter(
        IQueryable<County> query,
        IFilterDefinition<County> filter)
    {
        if (filter.Column?.PropertyName == null || filter.Operator == null)
            return query;

        var propertyName = filter.Column.PropertyName;
        var filterValue = filter.Value?.ToString()?.ToLower() ?? string.Empty;

        if (string.IsNullOrEmpty(filterValue))
            return query;

        return propertyName switch
        {
            "CountyName" => query.Where(c => c.CountyName != null && c.CountyName.ToLower().Contains(filterValue)),
            "ContactName" => query.Where(c => c.ContactName != null && c.ContactName.ToLower().Contains(filterValue)),
            "ContactEmail" => query.Where(c => c.ContactEmail != null && c.ContactEmail.ToLower().Contains(filterValue)),
            "ContactPhone" => query.Where(c => c.ContactPhone != null && c.ContactPhone.Contains(filterValue)),
            "ContactFax" => query.Where(c => c.ContactFax != null && c.ContactFax.Contains(filterValue)),
            "City" => query.Where(c => c.City != null && c.City.ToLower().Contains(filterValue)),
            "StateCode" => query.Where(c => c.StateCode != null && c.StateCode.ToLower().Contains(filterValue)),
            "ZipCode" => query.Where(c => c.ZipCode != null && c.ZipCode.Contains(filterValue)),
            _ => query
        };
    }

    private static IQueryable<County> ApplySorting(
        IQueryable<County> query,
        ICollection<SortDefinition<County>> sortDefinitions)
    {
        IOrderedQueryable<County>? orderedQuery = null;

        foreach (var sort in sortDefinitions)
        {
            if (sort.SortBy == null) continue;

            var propertyName = sort.SortBy;
            var descending = sort.Descending;

            if (orderedQuery == null)
            {
                orderedQuery = propertyName switch
                {
                    "CountyName" => descending
                        ? query.OrderByDescending(c => c.CountyName)
                        : query.OrderBy(c => c.CountyName),
                    "ContactName" => descending
                        ? query.OrderByDescending(c => c.ContactName)
                        : query.OrderBy(c => c.ContactName),
                    "ContactEmail" => descending
                        ? query.OrderByDescending(c => c.ContactEmail)
                        : query.OrderBy(c => c.ContactEmail),
                    "ContactPhone" => descending
                        ? query.OrderByDescending(c => c.ContactPhone)
                        : query.OrderBy(c => c.ContactPhone),
                    "City" => descending
                        ? query.OrderByDescending(c => c.City)
                        : query.OrderBy(c => c.City),
                    "StateCode" => descending
                        ? query.OrderByDescending(c => c.StateCode)
                        : query.OrderBy(c => c.StateCode),
                    "ZipCode" => descending
                        ? query.OrderByDescending(c => c.ZipCode)
                        : query.OrderBy(c => c.ZipCode),
                    _ => query.OrderBy(c => c.CountyName)
                };
            }
            else
            {
                orderedQuery = propertyName switch
                {
                    "CountyName" => descending
                        ? orderedQuery.ThenByDescending(c => c.CountyName)
                        : orderedQuery.ThenBy(c => c.CountyName),
                    "ContactName" => descending
                        ? orderedQuery.ThenByDescending(c => c.ContactName)
                        : orderedQuery.ThenBy(c => c.ContactName),
                    "ContactEmail" => descending
                        ? orderedQuery.ThenByDescending(c => c.ContactEmail)
                        : orderedQuery.ThenBy(c => c.ContactEmail),
                    "ContactPhone" => descending
                        ? orderedQuery.ThenByDescending(c => c.ContactPhone)
                        : orderedQuery.ThenBy(c => c.ContactPhone),
                    "City" => descending
                        ? orderedQuery.ThenByDescending(c => c.City)
                        : orderedQuery.ThenBy(c => c.City),
                    "StateCode" => descending
                        ? orderedQuery.ThenByDescending(c => c.StateCode)
                        : orderedQuery.ThenBy(c => c.StateCode),
                    "ZipCode" => descending
                        ? orderedQuery.ThenByDescending(c => c.ZipCode)
                        : orderedQuery.ThenBy(c => c.ZipCode),
                    _ => orderedQuery
                };
            }
        }

        return orderedQuery ?? query.OrderBy(c => c.CountyName);
    }

    private static string GetFullAddress(County county)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(county.AddressLine1))
            parts.Add(county.AddressLine1);
        if (!string.IsNullOrWhiteSpace(county.AddressLine2))
            parts.Add(county.AddressLine2);

        return string.Join(", ", parts);
    }

    #endregion
}