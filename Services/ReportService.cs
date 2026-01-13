using Microsoft.Extensions.Caching.Memory;
using SSRBlazor.Models;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for Report business logic
/// </summary>
public class ReportService
{
    private readonly ReportRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReportService> _logger;
    private readonly SSRBusiness.BusinessClasses.ReportService _businessReportService;

    private const string ReportsListCacheKey = "Reports_List";
    private const string CountiesCacheKey = "Reports_Counties";
    private const string OperatorsCacheKey = "Reports_Operators";
    private const string BuyersCacheKey = "Reports_Buyers";
    private const string DealStatusesCacheKey = "Reports_DealStatuses";
    private const string LandmenCacheKey = "Reports_Landmen";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly AcquisitionRepository _acquisitionRepository;

    public ReportService(
        ReportRepository repository,
        IMemoryCache cache,
        ILogger<ReportService> logger,
        SSRBusiness.BusinessClasses.ReportService businessReportService,
        AcquisitionRepository acquisitionRepository)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _businessReportService = businessReportService;
        _acquisitionRepository = acquisitionRepository;
    }

    #region Report Operations

    /// <summary>
    /// Get all active reports
    /// </summary>
    public async Task<List<Report>> GetAllReportsAsync()
    {
        return await _cache.GetOrCreateAsync(ReportsListCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _repository.GetAllAsync();
        }) ?? new List<Report>();
    }

    /// <summary>
    /// Get reports grouped by category
    /// </summary>
    public async Task<Dictionary<string, List<Report>>> GetReportsByCategoryAsync()
    {
        var reports = await GetAllReportsAsync();

        return reports
            .GroupBy(r => r.Category ?? "Other")
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Get report by ID
    /// </summary>
    public async Task<Report?> GetByIdAsync(int reportId)
    {
        return await _repository.GetByIdAsync(reportId);
    }

    #endregion

    #region Filter Lookup Data

    /// <summary>
    /// Get counties for filter dropdown
    /// </summary>
    public async Task<List<ReportLookupItem>> GetCountiesAsync()
    {
        return await _cache.GetOrCreateAsync(CountiesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var counties = await _repository.GetCountiesAsync();
            return counties.Select(c => new ReportLookupItem
            {
                Id = c.CountyID,
                Name = c.CountyName ?? string.Empty
            }).ToList();
        }) ?? new List<ReportLookupItem>();
    }

    /// <summary>
    /// Get operators for filter dropdown
    /// </summary>
    public async Task<List<ReportLookupItem>> GetOperatorsAsync()
    {
        return await _cache.GetOrCreateAsync(OperatorsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var operators = await _repository.GetOperatorsAsync();
            return operators.Select(o => new ReportLookupItem
            {
                Id = o.OperatorID,
                Name = o.OperatorName ?? string.Empty
            }).ToList();
        }) ?? new List<ReportLookupItem>();
    }

    /// <summary>
    /// Get buyers for filter dropdown
    /// </summary>
    public async Task<List<ReportLookupItem>> GetBuyersAsync()
    {
        return await _cache.GetOrCreateAsync(BuyersCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var buyers = await _repository.GetBuyersAsync();
            return buyers.Select(b => new ReportLookupItem
            {
                Id = b.BuyerID,
                Name = b.BuyerName ?? string.Empty
            }).ToList();
        }) ?? new List<ReportLookupItem>();
    }

    /// <summary>
    /// Get deal statuses for filter dropdown
    /// </summary>
    public async Task<List<ReportLookupItem>> GetDealStatusesAsync()
    {
        return await _cache.GetOrCreateAsync(DealStatusesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var statuses = await _repository.GetDealStatusesAsync();
            return statuses.Select(s => new ReportLookupItem
            {
                Id = s.FilterID,
                Name = s.FilterName ?? string.Empty
            }).ToList();
        }) ?? new List<ReportLookupItem>();
    }

    /// <summary>
    /// Get landmen for filter dropdown
    /// </summary>
    public async Task<List<ReportLookupItem>> GetLandmenAsync()
    {
        return await _cache.GetOrCreateAsync(LandmenCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var landmen = await _repository.GetLandmenAsync();
            return landmen.Select(l => new ReportLookupItem
            {
                Id = l.ReferrerID,
                Name = l.ReferrerName ?? string.Empty
            }).ToList();
        }) ?? new List<ReportLookupItem>();
    }

    /// <summary>
    /// Load all filter lookup data at once
    /// </summary>
    public async Task<ReportFilterLookups> GetAllLookupsAsync()
    {
        return new ReportFilterLookups
        {
            Counties = await GetCountiesAsync(),
            Operators = await GetOperatorsAsync(),
            Buyers = await GetBuyersAsync(),
            DealStatuses = await GetDealStatusesAsync(),
            Landmen = await GetLandmenAsync(),
            FieldLandmen = await GetLandmenAsync() // Same source as landmen
        };
    }

    #endregion

    #region Report Generation

    /// <summary>
    /// Validate report parameters
    /// </summary>
    public List<string> ValidateParameters(ReportParameters parameters)
    {
        var errors = new List<string>();

        // Validate date ranges
        if (!parameters.EffectiveDate.IsValid)
            errors.Add("Effective Date: " + parameters.EffectiveDate.ValidationError);

        if (!parameters.BuyerEffectiveDate.IsValid)
            errors.Add("Buyer Effective Date: " + parameters.BuyerEffectiveDate.ValidationError);

        if (!parameters.DueDate.IsValid)
            errors.Add("Due Date: " + parameters.DueDate.ValidationError);

        if (!parameters.PaidDate.IsValid)
            errors.Add("Paid Date: " + parameters.PaidDate.ValidationError);

        if (!parameters.TitleOpinionReceivedDate.IsValid)
            errors.Add("Title Opinion Received Date: " + parameters.TitleOpinionReceivedDate.ValidationError);

        if (!parameters.ClosingLetterDate.IsValid)
            errors.Add("Closing Letter Date: " + parameters.ClosingLetterDate.ValidationError);

        if (!parameters.DeedDate.IsValid)
            errors.Add("Deed Date: " + parameters.DeedDate.ValidationError);

        if (!parameters.InvoiceDate.IsValid)
            errors.Add("Invoice Date: " + parameters.InvoiceDate.ValidationError);

        if (!parameters.InvoiceDueDate.IsValid)
            errors.Add("Invoice Due Date: " + parameters.InvoiceDueDate.ValidationError);

        if (!parameters.InvoicePaidDate.IsValid)
            errors.Add("Invoice Paid Date: " + parameters.InvoicePaidDate.ValidationError);

        return errors;
    }

    /// <summary>
    /// Generate report URL with parameters
    /// </summary>
    public string GetReportUrl(Report report, ReportParameters parameters)
    {
        // Special routing for Drafts Due
        if (report.ReportID == 1)
        {
            var draftsDueParams = new List<string>();

            // Pass necessary params to Drafts Due page
            if (parameters.SelectedCountyIds.Any())
                draftsDueParams.Add($"counties={string.Join(",", parameters.SelectedCountyIds)}");

            if (!string.IsNullOrEmpty(parameters.SortBy))
                draftsDueParams.Add($"sortBy={parameters.SortBy}");

            return $"/reports/drafts-due?{string.Join("&", draftsDueParams)}";
        }

        // Build query string for report viewer
        var queryParams = new List<string>
        {
            $"reportId={report.ReportID}",
            $"format={parameters.OutputFormat}"
        };

        // Add multi-select parameters
        if (parameters.SelectedCountyIds.Any())
            queryParams.Add($"counties={string.Join(",", parameters.SelectedCountyIds)}");

        if (parameters.SelectedOperatorIds.Any())
            queryParams.Add($"operators={string.Join(",", parameters.SelectedOperatorIds)}");

        if (parameters.SelectedBuyerIds.Any())
            queryParams.Add($"buyers={string.Join(",", parameters.SelectedBuyerIds)}");

        if (parameters.SelectedDealStatusIds.Any())
        {
            queryParams.Add($"dealStatuses={string.Join(",", parameters.SelectedDealStatusIds)}");
            queryParams.Add($"dealStatusQueryType={parameters.DealStatusQueryType}");
        }

        if (parameters.SelectedLandmanIds.Any())
            queryParams.Add($"landmen={string.Join(",", parameters.SelectedLandmanIds)}");

        if (parameters.SelectedFieldLandmanIds.Any())
            queryParams.Add($"fieldLandmen={string.Join(",", parameters.SelectedFieldLandmanIds)}");

        // Add date range parameters
        AddDateRangeParams(queryParams, "effectiveDate", parameters.EffectiveDate);
        AddDateRangeParams(queryParams, "buyerEffectiveDate", parameters.BuyerEffectiveDate);
        AddDateRangeParams(queryParams, "dueDate", parameters.DueDate);
        AddDateRangeParams(queryParams, "paidDate", parameters.PaidDate);
        AddDateRangeParams(queryParams, "titleOpinionDate", parameters.TitleOpinionReceivedDate);
        AddDateRangeParams(queryParams, "closingLetterDate", parameters.ClosingLetterDate);
        AddDateRangeParams(queryParams, "deedDate", parameters.DeedDate);
        AddDateRangeParams(queryParams, "invoiceDate", parameters.InvoiceDate);
        AddDateRangeParams(queryParams, "invoiceDueDate", parameters.InvoiceDueDate);
        AddDateRangeParams(queryParams, "invoicePaidDate", parameters.InvoicePaidDate);

        // Add boolean filter parameters
        AddBooleanFilterParams(queryParams, "invoiceNumber", parameters.InvoiceNumber);
        AddBooleanFilterParams(queryParams, "isReferral", parameters.IsReferral);
        AddBooleanFilterParams(queryParams, "hasCuratives", parameters.HasCuratives);
        AddBooleanFilterParams(queryParams, "hasLiens", parameters.HasLiens);
        AddBooleanFilterParams(queryParams, "hasLetterAgreement", parameters.HasLetterAgreement);

        // Add report options
        queryParams.Add($"sortBy={parameters.SortBy}");
        queryParams.Add($"subtotalBy={parameters.SubtotalBy}");
        queryParams.Add($"summaryOnly={parameters.SummaryLevelOnly}");
        queryParams.Add($"includeNew={parameters.IncludeNewAcquisitions}");
        queryParams.Add($"includeAmountPaid={parameters.IncludeAmountPaidSellerSubtotal}");
        queryParams.Add($"includeNotes={parameters.IncludeNotes}");

        return $"/report/viewer?{string.Join("&", queryParams)}";
    }

    private void AddDateRangeParams(List<string> queryParams, string prefix, DateRangeFilter filter)
    {
        if (filter.FromDate.HasValue)
            queryParams.Add($"{prefix}From={filter.FromDate:yyyy-MM-dd}");

        if (filter.ToDate.HasValue)
            queryParams.Add($"{prefix}To={filter.ToDate:yyyy-MM-dd}");

        if (filter.IsEmpty)
            queryParams.Add($"{prefix}IsEmpty=true");

        if (filter.IsNotEmpty)
            queryParams.Add($"{prefix}IsNotEmpty=true");
    }

    private void AddBooleanFilterParams(List<string> queryParams, string prefix, BooleanFilter filter)
    {
        var value = filter.GetValue();
        if (value.HasValue)
            queryParams.Add($"{prefix}={value.Value}");
    }

    /// <summary>
    /// Generate report and return as byte array
    /// </summary>
    public async Task<(bool Success, byte[]? Data, string? ContentType, string? FileName, string? Error)> GenerateReportAsync(
        Report report,
        ReportParameters parameters)
    {
        try
        {
            // Validate parameters
            var errors = ValidateParameters(parameters);
            if (errors.Any())
            {
                return (false, null, null, null, string.Join("; ", errors));
            }

            _logger.LogInformation("Generating report {ReportId} with format {Format}",
                report.ReportID, parameters.OutputFormat);

            // 1. Get Filtered Acquisition IDs (if report is based on Acquisitions)
            var criteria = MapToCriteria(parameters);
            var acquisitions = await _acquisitionRepository.GetFilteredAcquisitionListAsync(criteria);
            var acqIds = acquisitions.Select(a => a.AcquisitionID.ToString()).ToList();

            byte[]? reportData = null;
            string extension = parameters.OutputFormat switch { "excel" => ".xlsx", "viewer" => ".pdf", "pdf" => ".pdf", "word" => ".docx", _ => ".html" };
            string contentType = parameters.OutputFormat switch { "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "viewer" => "application/pdf", "pdf" => "application/pdf", "word" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document", _ => "text/html" };

            // 2. Dispatch based on Report ID
            switch (report.ReportID)
            {
                case 1: // Drafts Due
                    reportData = await _businessReportService.RunDraftsDueReportAsync(acqIds, parameters.SortBy ?? "DueDate");
                    break;
                case 2: // Buyer Invoices Due
                    reportData = await _businessReportService.RunBuyerInvoicesDueReportAsync(acqIds);
                    break;
                case 3: // Curative Requirements
                    reportData = await _businessReportService.RunCurativeRequirementsReportAsync(acqIds);
                    break;
                case 4: // Letter Agreement Deals
                    // Note: Letter Agreement Deals usually takes LA IDs, but our service currently expects String IDs list
                    // If acqIds are appropriate, use them, or find LA IDs. 
                    // Assuming acqIds for now as it's the main driver.
                    reportData = await _businessReportService.RunLetterAgreementDealsReportAsync(acqIds);
                    break;
                case 5: // Purchases
                    reportData = await _businessReportService.RunPurchasesReportAsync(acqIds);
                    break;
                case 6: // Referrer 1099 Summary
                    // CurrentUserID is needed. Hardcoding "SYSTEM" or similar if not available in context.
                    // In a real app, this would come from AuthenticationStateProvider.
                    reportData = await _businessReportService.RunReferrer1099SummaryReportAsync("SYSTEM", acqIds);
                    break;
                default:
                    return (false, null, null, null, "Report ID not recognized.");
            }

            var fileName = $"{report.ReportName?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";

            return (true, reportData, contentType, fileName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report {ReportId}", report.ReportID);
            return (false, null, null, null, ex.Message);
        }
    }

    private SSRBusiness.ReportQueries.ReportSelectionCriteria MapToCriteria(ReportParameters p)
    {
        var c = new SSRBusiness.ReportQueries.ReportSelectionCriteria
        {
            LandmanList = p.SelectedLandmanIds.Select(id => id.ToString()).ToList(),
            FieldLandmanList = p.SelectedFieldLandmanIds.Select(id => id.ToString()).ToList(),
            CountyList = p.SelectedCountyIds.Select(id => id.ToString()).ToList(),
            OperatorList = p.SelectedOperatorIds.Select(id => id.ToString()).ToList(),
            BuyerList = p.SelectedBuyerIds.Select(id => id.ToString()).ToList(),
            DealQueryType = p.DealStatusQueryType.ToString(),
            DealStatusList = p.SelectedDealStatusIds.Select(id => id.ToString()).ToList(),
            
            SortOrder = p.SortBy,
            SubTotalBy = p.SubtotalBy,
            IncludeAmountPaidSellerSubtotal = p.IncludeAmountPaidSellerSubtotal ? "Y" : "N",
            SummaryLevelOnly = p.SummaryLevelOnly ? "Y" : "N",
            IncludeNotes = p.IncludeNotes ? "Y" : "N"
        };

        MapDate(p.EffectiveDate, c.EffectiveDate);
        MapDate(p.BuyerEffectiveDate, c.BuyerEffectiveDate);
        MapDate(p.DueDate, c.DueDate);
        MapDate(p.PaidDate, c.PaidDate);
        MapDate(p.TitleOpinionReceivedDate, c.TitleOpinionReceivedDate);
        MapDate(p.ClosingLetterDate, c.ClosingLetterDate);
        MapDate(p.DeedDate, c.DeedDate);
        MapDate(p.InvoiceDate, c.InvoiceDate);
        MapDate(p.InvoiceDueDate, c.InvoiceDueDate);
        MapDate(p.InvoicePaidDate, c.InvoicePaidDate);

        c.InvoiceNumber.CheckIsEmpty = p.InvoiceNumber.IsTrue;
        c.InvoiceNumber.CheckIsNotEmpty = p.InvoiceNumber.IsFalse;

        c.ReferralCheck.CheckExists = p.IsReferral.IsTrue;
        c.ReferralCheck.CheckNotExists = p.IsReferral.IsFalse;

        c.CurativeCheck.CheckExists = p.HasCuratives.IsTrue;
        c.CurativeCheck.CheckNotExists = p.HasCuratives.IsFalse;

        c.LienCheck.CheckExists = p.HasLiens.IsTrue;
        c.LienCheck.CheckNotExists = p.HasLiens.IsFalse;

        c.LetterAgreementCheck.CheckExists = p.HasLetterAgreement.IsTrue;
        c.LetterAgreementCheck.CheckNotExists = p.HasLetterAgreement.IsFalse;

        return c;
    }

    private void MapDate(DateRangeFilter src, SSRBusiness.ReportQueries.ReportDate dest)
    {
        dest.FromDate = src.FromDate;
        dest.ToDate = src.ToDate;
        dest.CheckIsEmpty = src.IsEmpty;
        dest.CheckIsNotEmpty = src.IsNotEmpty;
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Clear all cached lookup data
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(ReportsListCacheKey);
        _cache.Remove(CountiesCacheKey);
        _cache.Remove(OperatorsCacheKey);
        _cache.Remove(BuyersCacheKey);
        _cache.Remove(DealStatusesCacheKey);
        _cache.Remove(LandmenCacheKey);
    }

    #endregion
}

/// <summary>
/// Container for all filter lookup data
/// </summary>
public class ReportFilterLookups
{
    public List<ReportLookupItem> Counties { get; set; } = new();
    public List<ReportLookupItem> Operators { get; set; } = new();
    public List<ReportLookupItem> Buyers { get; set; } = new();
    public List<ReportLookupItem> DealStatuses { get; set; } = new();
    public List<ReportLookupItem> Landmen { get; set; } = new();
    public List<ReportLookupItem> FieldLandmen { get; set; } = new();
}