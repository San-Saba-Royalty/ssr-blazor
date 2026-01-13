using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for Acquisition operations with caching support
/// </summary>
public class AcquisitionService
{
    private readonly AcquisitionRepository _repository;
    private readonly CachedDataService<Acquisition> _cachedDataService;
    private readonly SsrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AcquisitionService> _logger;

    private const string CacheKeyPrefix = "Acquisition";
    private const string AllAcquisitionsCacheKey = $"{CacheKeyPrefix}_All";
    private const string FiltersCacheKey = $"{CacheKeyPrefix}_Filters";
    private const string ViewsCacheKey = $"{CacheKeyPrefix}_Views";

    public AcquisitionService(
        AcquisitionRepository repository,
        CachedDataService<Acquisition> cachedDataService,
        SsrDbContext context,
        IMemoryCache cache,
        ILogger<AcquisitionService> logger)
    {
        _repository = repository;
        _cachedDataService = cachedDataService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated acquisitions with filtering and sorting
    /// </summary>
    public async Task<PagedResult<Acquisition>> GetAcquisitionsPagedAsync(
        int page,
        int pageSize,
        string? activeFilter = null,
        List<SortDefinition>? sortDefinitions = null,
        List<FilterDefinition>? filterDefinitions = null)
    {
        try
        {
            var allAcquisitions = await _repository.GetAcquisitionListAsync();

            // Apply named filter
            var filtered = ApplyNamedFilter(allAcquisitions, activeFilter);

            // Apply column filters
            filtered = ApplyColumnFilters(filtered, filterDefinitions);

            // Get total count before pagination
            var totalCount = filtered.Count;

            // Apply sorting
            var sorted = ApplySorting(filtered, sortDefinitions);

            // Apply pagination
            var items = sorted
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<Acquisition>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated acquisitions");
            throw;
        }
    }

    /// <summary>
    /// Get acquisition by ID with caching
    /// </summary>
    public async Task<Acquisition?> GetByIdAsync(int acquisitionId)
    {
        return await _cachedDataService.GetByIdAsync(acquisitionId);
    }

    /// <summary>
    /// Get all acquisitions with optional filter (cached)
    /// </summary>
    public async Task<List<Acquisition>> GetAllAsync(Expression<Func<Acquisition, bool>>? filter = null)
    {
        return await _cachedDataService.GetAllAsync(filter);
    }

    /// <summary>
    /// Create a new acquisition
    /// </summary>
    public async Task<int> CreateAsync(Acquisition acquisition)
    {
        try
        {
            await _repository.AddAsync(acquisition);
            await _repository.SaveChangesAsync();

            InvalidateCache();

            _logger.LogInformation("Created acquisition {AcquisitionId}", acquisition.AcquisitionID);
            return acquisition.AcquisitionID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating acquisition");
            throw;
        }
    }

    /// <summary>
    /// Update an existing acquisition
    /// </summary>
    public async Task UpdateAsync(Acquisition acquisition)
    {
        try
        {
            _repository.Update(acquisition);
            await _repository.SaveChangesAsync();

            InvalidateCache(acquisition.AcquisitionID);

            _logger.LogInformation("Updated acquisition {AcquisitionId}", acquisition.AcquisitionID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating acquisition {AcquisitionId}", acquisition.AcquisitionID);
            throw;
        }
    }

    /// <summary>
    /// Delete an acquisition
    /// </summary>
    public async Task DeleteAsync(int acquisitionId)
    {
        try
        {
            var acquisition = await _repository.GetByIdAsync(acquisitionId);
            if (acquisition != null)
            {
                _repository.Delete(acquisition);
                await _repository.SaveChangesAsync();

                InvalidateCache(acquisitionId);

                _logger.LogInformation("Deleted acquisition {AcquisitionId}", acquisitionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting acquisition {AcquisitionId}", acquisitionId);
            throw;
        }
    }

    /// <summary>
    /// Copy an acquisition with options
    /// </summary>
    public async Task<int> CopyAsync(int acquisitionId, AcquisitionCopyOptions? options = null)
    {
        try
        {
            options ??= new AcquisitionCopyOptions();

            // Load full graph
            var source = await _repository.LoadAcquisitionByAcquisitionIDAsync(acquisitionId);
            if (source == null)
            {
                throw new InvalidOperationException($"Acquisition {acquisitionId} not found");
            }

            // Create a copy
            var copy = new Acquisition
            {
                // Core fields
                TotalBonus = options.CopyTotalBonus ? source.TotalBonus : 0,
                DraftFee = options.CopyDraftFee ? source.DraftFee : 0,
                TotalBonusAndFee = options.CopyTotal ? source.TotalBonusAndFee : 0,
                ConsiderationFee = options.CopyConsiderationFee ? source.ConsiderationFee : 0,
                TakeConsiderationFromTotal = options.CopyTakeConsiderationFromTotal && source.TakeConsiderationFromTotal,

                EffectiveDate = options.CopyEffectiveDate ? source.EffectiveDate : null,
                BuyerEffectiveDate = options.CopyBuyerEffectiveDate ? source.BuyerEffectiveDate : null,
                DueDate = options.CopyDueDate ? source.DueDate : null,
                PaidDate = options.CopyPaidDate ? source.PaidDate : null,

                DraftCheckNumber = options.CopyDraftCheckNumber ? source.DraftCheckNumber : null,
                // OldTaxCheck - property doesn't exist on Acquisition entity
                TaxesDue = options.CopyTaxesDue && source.TaxesDue,
                TaxAmountDue = options.CopyTaxAmountDue ? source.TaxAmountDue : 0,
                TaxAmountPaid = options.CopyTaxAmountPaid ? source.TaxAmountPaid : 0,

                // Title Info
                HaveCheckStub = options.CopyCheckStub ? source.HaveCheckStub : "No",
                CheckStubDesc = options.CopyCheckStub ? source.CheckStubDesc : null,
                FieldCheck = options.CopyFieldCheck && source.FieldCheck,
                LandManID = options.CopyLandMan ? source.LandManID : null,
                FieldLandmanID = options.CopyFieldLandman ? source.FieldLandmanID : null,
                Liens = options.CopyLiens && source.Liens,
                LienAmount = options.CopyLienAmount ? source.LienAmount : 0,
                TitleOpinion = options.CopyTitleOpinion ? source.TitleOpinion : null,
                ClosingStatus = options.CopyClosingStatus ? source.ClosingStatus : null,

                // Buyer Info
                Buyer = options.CopyBuyer ? source.Buyer : null,
                AcquisitionNumber = options.CopyAcquisitionNumber ? source.AcquisitionNumber : null,
                Assignee = options.CopyAssignee ? source.Assignee : null,

                // Invoice Info
                InvoiceNumber = options.CopyInvoiceNumber ? source.InvoiceNumber : null,
                InvoiceDate = options.CopyInvoiceDate ? source.InvoiceDate : null,
                InvoiceDueDate = options.CopyInvoiceDueDate ? source.InvoiceDueDate : null,
                InvoicePaidDate = options.CopyInvoicePaidDate ? source.InvoicePaidDate : null,
                InvoiceTotal = options.CopyInvoiceTotal ? source.InvoiceTotal : 0,
                Commission = options.CopyCommission ? source.Commission : 0,
                CommissionPercent = options.CopyCommissionPercent ? source.CommissionPercent : 0,
                AutoCalculateInvoice = options.CopyAutoCalculateInvoice && source.AutoCalculateInvoice,

                FolderLocation = source.FolderLocation
            };

            await _repository.AddAsync(copy);
            // Must save to get ID before adding children that need FK, though EF usually handles it if adding to collections.
            // But here we are manually creating new entities and they might reference copy.AcquisitionID explicitly.
            await _repository.SaveChangesAsync();

            if (options.CopySellerInformation && source.AcquisitionSellers != null)
            {
                foreach (var seller in source.AcquisitionSellers)
                {
                    _context.AcquisitionSellers.Add(new AcquisitionSeller
                    {
                        AcquisitionID = copy.AcquisitionID,
                        SellerName = seller.SellerName,
                        SellerLastName = seller.SellerLastName,
                        AddressLine1 = seller.AddressLine1,
                        AddressLine2 = seller.AddressLine2,
                        City = seller.City,
                        StateCode = seller.StateCode,
                        ZipCode = seller.ZipCode,
                        ContactPhone = seller.ContactPhone,
                        ContactFax = seller.ContactFax,
                        ContactEmail = seller.ContactEmail,
                        CreatedOn = DateTime.Now,
                        LastUpdatedOn = DateTime.Now
                    });
                }
            }

            if (options.CopyCounties && source.AcquisitionCounties != null)
            {
                foreach (var item in source.AcquisitionCounties)
                {
                    _context.AcquisitionCounties.Add(new AcquisitionCounty
                    {
                        AcquisitionID = copy.AcquisitionID,
                        CountyID = item.CountyID,
                        DeedSentDate = item.DeedSentDate,
                        DeedReturnedDate = item.DeedReturnedDate,
                        RecordingBook = item.RecordingBook,
                        RecordingPage = item.RecordingPage
                    });
                }
            }

            if (options.CopyOperators && source.AcquisitionOperators != null)
            {
                foreach (var item in source.AcquisitionOperators)
                {
                    _context.AcquisitionOperators.Add(new AcquisitionOperator
                    {
                        AcquisitionID = copy.AcquisitionID,
                        OperatorID = item.OperatorID,
                        NotifiedDateNoRec = item.NotifiedDateNoRec,
                        NotifiedDateRec = item.NotifiedDateRec,
                        DOReceivedDate = item.DOReceivedDate
                    });
                }
            }

            if (options.CopyNotes && source.AcquisitionNotes != null)
            {
                foreach (var item in source.AcquisitionNotes)
                {
                    _context.AcquisitionNotes.Add(new AcquisitionNote
                    {
                        AcquisitionID = copy.AcquisitionID,
                        NoteText = item.NoteText,
                        NoteTypeCode = item.NoteTypeCode,
                        CreatedDateTime = DateTime.Now,
                        UserID = item.UserID
                    });
                }
            }

            if (options.CopyUnits && source.AcquisitionUnits != null)
            {
                foreach (var unit in source.AcquisitionUnits)
                {
                    var newUnit = new AcquisitionUnit
                    {
                        AcquisitionID = copy.AcquisitionID,
                        UnitName = unit.UnitName,
                        UnitTypeCode = unit.UnitTypeCode,
                        SsrInPay = unit.SsrInPay,
                        UnitInterest = unit.UnitInterest,
                        GrossAcres = unit.GrossAcres,
                        NetAcres = unit.NetAcres,
                        LegalDescription = unit.LegalDescription,
                        TownshipNum = unit.TownshipNum,
                        TownshipDir = unit.TownshipDir,
                        SectionNum = unit.SectionNum,
                        RangeNum = unit.RangeNum,
                        RangeDir = unit.RangeDir,
                        VolumeNumber = unit.VolumeNumber,
                        PageNumber = unit.PageNumber
                    };

                    // Add Unit first to get ID/Tracking? No, add to context later.
                    // Copy sub-collections
                    if (unit.AcqUnitCounties != null)
                    {
                        foreach (var ac in unit.AcqUnitCounties)
                        {
                            newUnit.AcqUnitCounties.Add(new AcqUnitCounty
                            {
                                AcquisitionID = copy.AcquisitionID,
                                CountyID = ac.CountyID
                                // AcquisitionUnit will be newUnit
                            });
                        }
                    }

                    if (unit.AcqUnitWells != null)
                    {
                        foreach (var aw in unit.AcqUnitWells)
                        {
                            newUnit.AcqUnitWells.Add(new AcqUnitWell
                            {
                                AcquisitionID = copy.AcquisitionID,
                                WellName = aw.WellName
                            });
                        }
                    }

                    _context.AcquisitionUnits.Add(newUnit);
                }
            }

            if (options.CopyLiens && source.AcquisitionLiens != null)
            {
                foreach (var item in source.AcquisitionLiens)
                {
                    _context.AcquisitionLiens.Add(new AcquisitionLien
                    {
                        AcquisitionID = copy.AcquisitionID,
                        LienHolder = item.LienHolder,
                        LienTypeID = item.LienTypeID,
                        LienPosition = item.LienPosition,
                        OriginalAmount = item.OriginalAmount,
                        PayoffAmount = item.PayoffAmount
                    });
                }
            }

            if (source.AcqCurativeRequirements != null) // If we want to copy curative? Options missing but good to have
            {
                // Legacy might not copy curative, but if we wanted to:
                // Not in options, ensuring parity means omitting unless known.
            }

            InvalidateCache();

            _logger.LogInformation("Copied acquisition {SourceId} to {NewId}", acquisitionId, copy.AcquisitionID);
            return copy.AcquisitionID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying acquisition {AcquisitionId}", acquisitionId);
            throw;
        }
    }

    /// <summary>
    /// Generate barcode cover sheet URL
    /// </summary>
    public Task<string> GenerateBarcodeCoverSheetAsync(int acquisitionId)
    {
        // Return the URL for the barcode generation endpoint
        return Task.FromResult($"/api/documents/barcode/acquisition/{acquisitionId}");
    }

    /// <summary>
    /// Export acquisitions to Excel bytes
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(string? activeFilter = null)
    {
        var allAcquisitions = await _repository.GetAcquisitionListAsync();
        var filtered = ApplyNamedFilter(allAcquisitions, activeFilter);

        // Use a library like ClosedXML or EPPlus to generate Excel
        // This is a placeholder - implement with your preferred Excel library
        return await GenerateExcelBytes(filtered.ToList());
    }

    /// <summary>
    /// Get available filters
    /// </summary>
    public async Task<List<NamedFilter>> GetAvailableFiltersAsync()
    {
        return await _cache.GetOrCreateAsync(FiltersCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            // Load from database or return static list
            return await Task.FromResult(new List<NamedFilter>
            {
                new("All Records", null),
                new("Pending Approval", a => a.ClosingStatus == "Pending"),
                new("Drafts Due", a => a.DueDate <= DateTime.Today && a.PaidDate == null),
                new("Invoices Due", a => a.InvoiceDueDate <= DateTime.Today && a.InvoicePaidDate == null),
                new("With Liens", a => a.Liens),
                new("In Pay", a => a.SsrInPay == "Yes")
            });
        }) ?? new List<NamedFilter>();
    }

    /// <summary>
    /// Get available views
    /// </summary>
    public async Task<List<NamedView>> GetAvailableViewsAsync()
    {
        return await _cache.GetOrCreateAsync(ViewsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            return await Task.FromResult(new List<NamedView>
            {
                new("All Fields", null),
                new("Summary View", new[] { "AcquisitionID", "Buyer", "TotalBonus", "EffectiveDate" }),
                new("Financial View", new[] { "AcquisitionID", "TotalBonus", "ConsiderationFee", "DraftFee", "InvoiceTotal", "PaidDate", "InvoicePaidDate" }),
                new("Title View", new[] { "AcquisitionID", "TitleOpinion", "Liens", "LienAmount", "ClosingStatus" }),
                new("Acreage View", new[] { "AcquisitionID", "TotalGrossAcres", "TotalNetAcres", "FieldCheck" })
            });
        }) ?? new List<NamedView>();
    }

    #region Private Helper Methods

    private List<Acquisition> ApplyNamedFilter(List<Acquisition> acquisitions, string? filterName)
    {
        if (string.IsNullOrEmpty(filterName) || filterName == "All Records")
            return acquisitions;

        return filterName switch
        {
            "Pending Approval" => acquisitions.Where(a => a.ClosingStatus == "Pending").ToList(),
            "Drafts Due" => acquisitions.Where(a => a.DueDate <= DateTime.Today && a.PaidDate == null).ToList(),
            "Invoices Due" => acquisitions.Where(a => a.InvoiceDueDate <= DateTime.Today && a.InvoicePaidDate == null).ToList(),
            "With Liens" => acquisitions.Where(a => a.Liens).ToList(),
            "In Pay" => acquisitions.Where(a => a.SsrInPay == "Yes").ToList(),
            _ => acquisitions
        };
    }

    private List<Acquisition> ApplyColumnFilters(List<Acquisition> acquisitions, List<FilterDefinition>? filters)
    {
        if (filters == null || !filters.Any())
            return acquisitions;

        var result = acquisitions.AsEnumerable();
        foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
        {
            result = ApplyFilter(result, filter);
        }

        return result.ToList();
    }

    private IEnumerable<Acquisition> ApplyFilter(IEnumerable<Acquisition> acquisitions, FilterDefinition filter)
    {
        // Dynamic filter application based on field name
        var value = filter.Value?.ToLower() ?? string.Empty;

        return filter.Field switch
        {
            "AcquisitionID" when int.TryParse(filter.Value, out var id) =>
                acquisitions.Where(a => a.AcquisitionID == id),

            "Buyer" =>
                acquisitions.Where(a => a.Buyer != null && a.Buyer.ToLower().Contains(value)),

            "AcquisitionNumber" =>
                acquisitions.Where(a => a.AcquisitionNumber != null && a.AcquisitionNumber.ToLower().Contains(value)),

            "Assignee" =>
                acquisitions.Where(a => a.Assignee != null && a.Assignee.ToLower().Contains(value)),

            "DraftCheckNumber" =>
                acquisitions.Where(a => a.DraftCheckNumber != null && a.DraftCheckNumber.Contains(value)),

            "InvoiceNumber" =>
                acquisitions.Where(a => a.InvoiceNumber != null && a.InvoiceNumber.Contains(value)),

            "Liens" when bool.TryParse(filter.Value, out var hasLiens) =>
                acquisitions.Where(a => a.Liens == hasLiens),

            "SsrInPay" =>
                acquisitions.Where(a => a.SsrInPay != null && a.SsrInPay.ToLower().Contains(value)),

            "ClosingStatus" =>
                acquisitions.Where(a => a.ClosingStatus != null && a.ClosingStatus.ToLower().Contains(value)),

            "TitleOpinion" =>
                acquisitions.Where(a => a.TitleOpinion != null && a.TitleOpinion.ToLower().Contains(value)),

            "FolderLocation" =>
                acquisitions.Where(a => a.FolderLocation != null && a.FolderLocation.ToLower().Contains(value)),

            _ => acquisitions
        };
    }

    private IEnumerable<Acquisition> ApplySorting(List<Acquisition> acquisitions, List<SortDefinition>? sortDefinitions)
    {
        if (sortDefinitions == null || !sortDefinitions.Any())
        {
            // Default sort by AcquisitionID descending
            return acquisitions.OrderByDescending(a => a.AcquisitionID);
        }

        IOrderedEnumerable<Acquisition>? orderedResult = null;

        foreach (var sort in sortDefinitions)
        {
            orderedResult = ApplySort(orderedResult ?? acquisitions.OrderBy(a => 0), sort, orderedResult == null);
        }

        return orderedResult ?? acquisitions.AsEnumerable();
    }

    private IOrderedEnumerable<Acquisition> ApplySort(IOrderedEnumerable<Acquisition> acquisitions, SortDefinition sort, bool isFirst)
    {
        // Dynamic sorting
        return sort.SortBy switch
        {
            "AcquisitionID" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.AcquisitionID) : acquisitions.OrderBy(a => a.AcquisitionID))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.AcquisitionID) : acquisitions.ThenBy(a => a.AcquisitionID)),

            "Buyer" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.Buyer) : acquisitions.OrderBy(a => a.Buyer))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.Buyer) : acquisitions.ThenBy(a => a.Buyer)),

            "AcquisitionNumber" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.AcquisitionNumber) : acquisitions.OrderBy(a => a.AcquisitionNumber))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.AcquisitionNumber) : acquisitions.ThenBy(a => a.AcquisitionNumber)),

            "EffectiveDate" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.EffectiveDate) : acquisitions.OrderBy(a => a.EffectiveDate))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.EffectiveDate) : acquisitions.ThenBy(a => a.EffectiveDate)),

            "DueDate" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.DueDate) : acquisitions.OrderBy(a => a.DueDate))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.DueDate) : acquisitions.ThenBy(a => a.DueDate)),

            "PaidDate" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.PaidDate) : acquisitions.OrderBy(a => a.PaidDate))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.PaidDate) : acquisitions.ThenBy(a => a.PaidDate)),

            "TotalBonus" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.TotalBonus) : acquisitions.OrderBy(a => a.TotalBonus))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.TotalBonus) : acquisitions.ThenBy(a => a.TotalBonus)),

            "InvoiceTotal" => isFirst
                ? (sort.Descending ? acquisitions.OrderByDescending(a => a.InvoiceTotal) : acquisitions.OrderBy(a => a.InvoiceTotal))
                : (sort.Descending ? acquisitions.ThenByDescending(a => a.InvoiceTotal) : acquisitions.ThenBy(a => a.InvoiceTotal)),

            _ => acquisitions
        };
    }


    private void InvalidateCache(int? acquisitionId = null)
    {
        if (acquisitionId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}_{acquisitionId}");
        }

        _cache.Remove(AllAcquisitionsCacheKey);
        _cachedDataService.InvalidateCache();

        _logger.LogInformation("Cache invalidated for Acquisitions");
    }

    private async Task<byte[]> GenerateExcelBytes(List<Acquisition> acquisitions)
    {
        // Placeholder - implement with ClosedXML or EPPlus
        // Example with ClosedXML:
        // using var workbook = new XLWorkbook();
        // var worksheet = workbook.Worksheets.Add("Acquisitions");
        // worksheet.Cell(1, 1).InsertTable(acquisitions);
        // using var stream = new MemoryStream();
        // workbook.SaveAs(stream);
        // return stream.ToArray();

        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Named filter definition
/// </summary>
public class NamedFilter
{
    public string Name { get; }
    public Expression<Func<Acquisition, bool>>? Predicate { get; }

    public NamedFilter(string name, Expression<Func<Acquisition, bool>>? predicate)
    {
        Name = name;
        Predicate = predicate;
    }
}

/// <summary>
/// Named view definition
/// </summary>
public class NamedView
{
    public string Name { get; }
    public string[]? VisibleColumns { get; }

    public NamedView(string name, string[]? visibleColumns)
    {
        Name = name;
        VisibleColumns = visibleColumns;
    }
}

/// <summary>
/// Sort definition for grid
/// </summary>
public class SortDefinition
{
    public string SortBy { get; set; } = string.Empty;
    public bool Descending { get; set; }
}

/// <summary>
/// Filter definition for grid
/// </summary>
public class FilterDefinition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "Contains";
    public string? Value { get; set; }
}

/// <summary>
/// Paged result wrapper
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
}

/// <summary>
/// Options for copying an acquisition
/// </summary>
public class AcquisitionCopyOptions
{
    // Seller
    public bool CopySellerInformation { get; set; } = true;

    // Deed & Draft
    public bool CopyEffectiveDate { get; set; } = true;
    public bool CopyBuyerEffectiveDate { get; set; } = true;
    public bool CopyDueDate { get; set; } = true;
    public bool CopyPaidDate { get; set; } = true;
    public bool CopyTotalBonus { get; set; } = true;
    public bool CopyConsiderationFee { get; set; } = true;
    public bool CopyTakeConsiderationFromTotal { get; set; } = true;
    // public bool CopyReferralInfo { get; set; } = true; 
    public bool CopyTotal { get; set; } = true;
    public bool CopyDraftFee { get; set; } = true;
    public bool CopyDraftCheckNumber { get; set; } = true;
    public bool CopyOldTaxCheck { get; set; } = true;
    public bool CopyTaxesDue { get; set; } = true;
    public bool CopyTaxAmountDue { get; set; } = true;
    public bool CopyTaxAmountPaid { get; set; } = true;

    // Title
    public bool CopyCheckStub { get; set; } = true;
    public bool CopyFieldCheck { get; set; } = true;
    public bool CopyLandMan { get; set; } = true;
    public bool CopyFieldLandman { get; set; } = true;
    public bool CopyLiens { get; set; } = true;
    public bool CopyLienAmount { get; set; } = true;
    public bool CopyTitleOpinion { get; set; } = true;
    public bool CopyClosingStatus { get; set; } = true;

    // Buyer
    public bool CopyBuyer { get; set; } = true;
    public bool CopyAcquisitionNumber { get; set; } = true;
    public bool CopyAssignee { get; set; } = true;

    // Invoice
    public bool CopyInvoiceNumber { get; set; } = true;
    public bool CopyInvoiceDate { get; set; } = true;
    public bool CopyInvoiceDueDate { get; set; } = true;
    public bool CopyInvoicePaidDate { get; set; } = true;
    public bool CopyInvoiceTotal { get; set; } = true;
    public bool CopyAutoCalculateInvoice { get; set; } = true;
    public bool CopyCommissionPercent { get; set; } = true;
    public bool CopyCommission { get; set; } = true;

    // Sub-entities
    public bool CopyNotes { get; set; } = true;
    public bool CopyUnits { get; set; } = true;
    public bool CopyCounties { get; set; } = true;
    public bool CopyOperators { get; set; } = true;
}

#endregion