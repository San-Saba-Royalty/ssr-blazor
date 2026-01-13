using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for Filter operations - manages saved filters and filter criteria
/// </summary>
public class FilterService
{
    private readonly FilterRepository _filterRepository;
    private readonly FilterFieldRepository _filterFieldRepository;
    private readonly SsrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FilterService> _logger;

    private const string CacheKeyPrefix = "Filter";
    private const string AllFiltersCacheKey = $"{CacheKeyPrefix}_All";
    private const string FieldsCacheKey = $"{CacheKeyPrefix}_Fields";
    private const string LookupFieldsCacheKey = $"{CacheKeyPrefix}_LookupFields";
    private const string ComparisonTypesCacheKey = $"{CacheKeyPrefix}_ComparisonTypes";

    public FilterService(
        FilterRepository filterRepository,
        FilterFieldRepository filterFieldRepository,
        SsrDbContext context,
        IMemoryCache cache,
        ILogger<FilterService> logger)
    {
        _filterRepository = filterRepository;
        _filterFieldRepository = filterFieldRepository;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    #region Filter CRUD Operations

    /// <summary>
    /// Get all filters for dropdown selection
    /// </summary>
    public async Task<List<Filter>> GetAllFiltersAsync()
    {
        return await _cache.GetOrCreateAsync(AllFiltersCacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            var query = await _filterRepository.GetFiltersAsync();
            return await query.OrderBy(f => f.FilterName).ToListAsync();
        }) ?? new List<Filter>();
    }

    /// <summary>
    /// Get filter by ID
    /// </summary>
    public async Task<Filter?> GetFilterByIdAsync(int filterId)
    {
        return await _filterRepository.GetFilterByIdAsync(filterId);
    }

    /// <summary>
    /// Get filter fields by filter ID and convert to view models
    /// </summary>
    public async Task<List<FilterDetailModel>> GetFilterFieldsAsync(int filterId)
    {
        var filterFields = await _filterFieldRepository.GetByFilterIdAsync(filterId);
        var lookupFields = await GetLookupFieldsAsync();
        var comparisonTypes = await GetComparisonTypesAsync();

        var models = new List<FilterDetailModel>();
        int rowNumber = 1;

        foreach (var field in filterFields)
        {
            var lookupField = lookupFields.FirstOrDefault(lf => lf.FieldID == field.FieldID);
            var comparisonType = comparisonTypes.FirstOrDefault(ct => ct.ComparisonTypeID == field.ComparisonTypeID);

            models.Add(new FilterDetailModel
            {
                RowNumber = rowNumber++,
                FieldName = lookupField?.FieldName,
                ComparisonType = comparisonType?.ComparisonDesc,
                CompareValue = field.ComparisonValue
            });
        }

        return models;
    }

    /// <summary>
    /// Create a new filter with details
    /// </summary>
    public async Task<int> CreateFilterAsync(Filter filter, List<FilterDetailModel> details)
    {
        try
        {
            // Create the filter
            await _filterRepository.AddAsync(filter);
            await _filterRepository.SaveChangesAsync();

            // Load lookup data for mapping
            var lookupFields = await GetLookupFieldsAsync();
            var comparisonTypes = await GetComparisonTypesAsync();

            // Create the filter fields
            foreach (var detail in details.Where(d => !string.IsNullOrEmpty(d.FieldName)))
            {
                var lookupField = lookupFields.FirstOrDefault(lf => lf.FieldName == detail.FieldName);
                var comparisonType = comparisonTypes.FirstOrDefault(ct => ct.ComparisonDesc == detail.ComparisonType);

                if (lookupField == null || comparisonType == null)
                {
                    _logger.LogWarning("Skipping filter field - could not find lookup data for FieldName: {FieldName}, ComparisonType: {ComparisonType}",
                        detail.FieldName, detail.ComparisonType);
                    continue;
                }

                var filterField = new FilterField
                {
                    FilterID = filter.FilterID,
                    ConditionalCode = "AND", // Default to AND for now
                    FieldID = lookupField.FieldID,
                    ComparisonTypeID = comparisonType.ComparisonTypeID,
                    ComparisonValue = detail.CompareValue
                };
                await _filterFieldRepository.AddAsync(filterField);
            }
            await _filterFieldRepository.SaveChangesAsync();

            InvalidateCache();

            _logger.LogInformation("Created filter {FilterId}: {FilterName}", filter.FilterID, filter.FilterName);
            return filter.FilterID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating filter");
            throw;
        }
    }

    /// <summary>
    /// Update an existing filter with details
    /// </summary>
    public async Task UpdateFilterAsync(Filter filter, List<FilterDetailModel> details)
    {
        try
        {
            // Update the filter
            _filterRepository.Update(filter);

            // Delete existing fields
            await _filterFieldRepository.DeleteByFilterIdAsync(filter.FilterID);
            await _filterFieldRepository.SaveChangesAsync();

            // Load lookup data for mapping
            var lookupFields = await GetLookupFieldsAsync();
            var comparisonTypes = await GetComparisonTypesAsync();

            // Create new fields
            foreach (var detail in details.Where(d => !string.IsNullOrEmpty(d.FieldName)))
            {
                var lookupField = lookupFields.FirstOrDefault(lf => lf.FieldName == detail.FieldName);
                var comparisonType = comparisonTypes.FirstOrDefault(ct => ct.ComparisonDesc == detail.ComparisonType);

                if (lookupField == null || comparisonType == null)
                {
                    _logger.LogWarning("Skipping filter field - could not find lookup data for FieldName: {FieldName}, ComparisonType: {ComparisonType}",
                        detail.FieldName, detail.ComparisonType);
                    continue;
                }

                var filterField = new FilterField
                {
                    FilterID = filter.FilterID,
                    ConditionalCode = "AND", // Default to AND for now
                    FieldID = lookupField.FieldID,
                    ComparisonTypeID = comparisonType.ComparisonTypeID,
                    ComparisonValue = detail.CompareValue
                };
                await _filterFieldRepository.AddAsync(filterField);
            }

            await _filterRepository.SaveChangesAsync();
            await _filterFieldRepository.SaveChangesAsync();

            InvalidateCache();

            _logger.LogInformation("Updated filter {FilterId}: {FilterName}", filter.FilterID, filter.FilterName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating filter {FilterId}", filter.FilterID);
            throw;
        }
    }

    /// <summary>
    /// Delete a filter and its details
    /// </summary>
    public async Task DeleteFilterAsync(int filterId)
    {
        try
        {
            // Delete fields first
            await _filterFieldRepository.DeleteByFilterIdAsync(filterId);
            await _filterFieldRepository.SaveChangesAsync();

            // Delete the filter
            var filter = await _filterRepository.GetByIdAsync(filterId);
            if (filter != null)
            {
                _filterRepository.Delete(filter);
                await _filterRepository.SaveChangesAsync();
            }

            InvalidateCache();

            _logger.LogInformation("Deleted filter {FilterId}", filterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting filter {FilterId}", filterId);
            throw;
        }
    }

    #endregion

    #region Lookup Data

    /// <summary>
    /// Get lookup fields from database
    /// </summary>
    private async Task<List<LookupField>> GetLookupFieldsAsync()
    {
        return await _cache.GetOrCreateAsync(LookupFieldsCacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return await _context.LookupFields.ToListAsync();
        }) ?? new List<LookupField>();
    }

    /// <summary>
    /// Get comparison types from database
    /// </summary>
    private async Task<List<ComparisonType>> GetComparisonTypesAsync()
    {
        return await _cache.GetOrCreateAsync(ComparisonTypesCacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return await _context.ComparisonTypes.ToListAsync();
        }) ?? new List<ComparisonType>();
    }

    #endregion

    #region Field and Comparison Type Methods

    /// <summary>
    /// Get available fields for filtering (based on Acquisition entity)
    /// </summary>
    public List<FilterFieldDefinition> GetAvailableFields()
    {
        return _cache.GetOrCreate(FieldsCacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return GetFieldDefinitions();
        }) ?? new List<FilterFieldDefinition>();
    }

    /// <summary>
    /// Get comparison types for a specific field
    /// </summary>
    public List<ComparisonTypeOption> GetComparisonTypes(string fieldName)
    {
        var field = GetAvailableFields().FirstOrDefault(f => f.FieldName == fieldName);
        if (field == null)
            return GetDefaultComparisonTypes();

        return field.FieldType switch
        {
            FieldType.String => GetStringComparisonTypes(),
            FieldType.Number => GetNumericComparisonTypes(),
            FieldType.Decimal => GetNumericComparisonTypes(),
            FieldType.Date => GetDateComparisonTypes(),
            FieldType.Boolean => GetBooleanComparisonTypes(),
            _ => GetDefaultComparisonTypes()
        };
    }

    private List<FilterFieldDefinition> GetFieldDefinitions()
    {
        // Define available filter fields based on Acquisition entity
        return new List<FilterFieldDefinition>
        {
            // Primary fields
            new("AcquisitionID", "Acquisition ID", FieldType.Number),
            new("AcquisitionName", "Acquisition Name", FieldType.String),
            new("DealStatus", "Deal Status", FieldType.String),
            new("LandMan", "Land Man", FieldType.String),
            
            // Seller fields
            new("SellerLastName", "Seller Last Name", FieldType.String),
            new("SellerName", "Seller Name", FieldType.String),
            new("SellerEmail", "Seller Email", FieldType.String),
            new("SellerPhone", "Seller Phone", FieldType.String),
            new("SellerCity", "Seller City", FieldType.String),
            new("SellerState", "Seller State", FieldType.String),
            new("SellerZipCode", "Seller Zip Code", FieldType.String),
            
            // Location fields
            new("CountyName", "County", FieldType.String),
            new("OperatorName", "Operator", FieldType.String),
            new("UnitName", "Unit Name", FieldType.String),
            new("UnitType", "Unit Type", FieldType.String),
            new("Surveys", "Surveys", FieldType.String),
            
            // Financial fields
            new("TotalBonus", "Total Bonus", FieldType.Decimal),
            new("ConsiderationFee", "Consideration Fee", FieldType.Decimal),
            new("ReferralFee", "Referral Fee", FieldType.Decimal),
            new("TotalBonusAndFee", "Total Bonus and Fee", FieldType.Decimal),
            
            // Acreage fields
            new("TotalGrossAcres", "Total Gross Acres", FieldType.Decimal),
            new("TotalNetAcres", "Total Net Acres", FieldType.Decimal),
            new("UnitGrossAcres", "Unit Gross Acres", FieldType.Decimal),
            new("UnitNetAcres", "Unit Net Acres", FieldType.Decimal),
            
            // Date fields
            new("CreatedOn", "Created Date", FieldType.Date),
            new("EffectiveDate", "Effective Date", FieldType.Date),
            new("ReceiptDate", "Receipt Date", FieldType.Date),
            new("ClosingDate", "Closing Date", FieldType.Date),
            
            // Numeric fields
            new("ClosingDays", "Closing Days", FieldType.Number),
            
            // Boolean fields
            new("IsActive", "Is Active", FieldType.Boolean),
            new("TakeConsiderationFromTotal", "Take Consideration From Total", FieldType.Boolean),
            new("TakeReferralFeeFromTotal", "Take Referral Fee From Total", FieldType.Boolean)
        };
    }

    private List<ComparisonTypeOption> GetStringComparisonTypes()
    {
        return new List<ComparisonTypeOption>
        {
            new("Contains", "Contains"),
            new("DoesNotContain", "Does Not Contain"),
            new("StartsWith", "Starts With"),
            new("EndsWith", "Ends With"),
            new("Equals", "Equals"),
            new("NotEquals", "Not Equals"),
            new("IsEmpty", "Is Empty"),
            new("IsNotEmpty", "Is Not Empty")
        };
    }

    private List<ComparisonTypeOption> GetNumericComparisonTypes()
    {
        return new List<ComparisonTypeOption>
        {
            new("Equals", "Equals"),
            new("NotEquals", "Not Equals"),
            new("GreaterThan", "Greater Than"),
            new("GreaterThanOrEqual", "Greater Than or Equal"),
            new("LessThan", "Less Than"),
            new("LessThanOrEqual", "Less Than or Equal"),
            new("Between", "Between"),
            new("IsNull", "Is Null"),
            new("IsNotNull", "Is Not Null")
        };
    }

    private List<ComparisonTypeOption> GetDateComparisonTypes()
    {
        return new List<ComparisonTypeOption>
        {
            new("Equals", "Equals"),
            new("NotEquals", "Not Equals"),
            new("After", "After"),
            new("AfterOrEqual", "On or After"),
            new("Before", "Before"),
            new("BeforeOrEqual", "On or Before"),
            new("Between", "Between"),
            new("LastNDays", "Last N Days"),
            new("NextNDays", "Next N Days"),
            new("IsNull", "Is Null"),
            new("IsNotNull", "Is Not Null")
        };
    }

    private List<ComparisonTypeOption> GetBooleanComparisonTypes()
    {
        return new List<ComparisonTypeOption>
        {
            new("IsTrue", "Is True"),
            new("IsFalse", "Is False"),
            new("IsNull", "Is Null")
        };
    }

    private List<ComparisonTypeOption> GetDefaultComparisonTypes()
    {
        return new List<ComparisonTypeOption>
        {
            new("Contains", "Contains"),
            new("Equals", "Equals")
        };
    }

    #endregion

    #region Apply Filter

    /// <summary>
    /// Apply filter to redirect to Acquisitions with filter parameters
    /// </summary>
    public string BuildFilterQueryString(int filterId)
    {
        return $"?filterId={filterId}";
    }

    #endregion

    #region Private Methods

    private void InvalidateCache()
    {
        _cache.Remove(AllFiltersCacheKey);
        _logger.LogInformation("Cache invalidated for Filters");
    }

    #endregion
}

#region Supporting Classes

public class FilterDetailModel
{
    public int RowNumber { get; set; }
    public string? FieldName { get; set; }
    public string? ComparisonType { get; set; }
    public string? CompareValue { get; set; }
}

public class FilterFieldDefinition
{
    public string FieldName { get; }
    public string DisplayName { get; }
    public FieldType FieldType { get; }

    public FilterFieldDefinition(string fieldName, string displayName, FieldType fieldType)
    {
        FieldName = fieldName;
        DisplayName = displayName;
        FieldType = fieldType;
    }
}

public class ComparisonTypeOption
{
    public string Value { get; }
    public string Description { get; }

    public ComparisonTypeOption(string value, string description)
    {
        Value = value;
        Description = description;
    }
}

public enum FieldType
{
    String,
    Number,
    Decimal,
    Date,
    Boolean
}

#endregion
