using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;
using SSRBlazor.Components.Pages.LetterAgreements.Models;

namespace SSRBlazor.Services;

/// <summary>
/// Service for LetterAgreement operations with caching support
/// </summary>
public class LetterAgreementService
{
    private readonly LetterAgreementRepository _repository;
    private readonly CachedDataService<LetterAgreement> _cachedDataService;
    private readonly SsrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LetterAgreementService> _logger;

    private const string CacheKeyPrefix = "LetterAgreement";
    private const string AllLetterAgreementsCacheKey = $"{CacheKeyPrefix}_All";

    public LetterAgreementService(
        LetterAgreementRepository repository,
        CachedDataService<LetterAgreement> cachedDataService,
        SsrDbContext context,
        IMemoryCache cache,
        ILogger<LetterAgreementService> logger)
    {
        _repository = repository;
        _cachedDataService = cachedDataService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated letter agreements with filtering and sorting (returns ViewModels for display)
    /// </summary>
    public async Task<PagedResult<LetterAgreementViewModel>> GetLetterAgreementsPagedAsync(
        int page,
        int pageSize,
        List<SortDefinition>? sortDefinitions = null,
        List<FilterDefinition>? filterDefinitions = null)
    {
        try
        {
            // Build query - Project to ViewModel with seller and related data
            var viewModelQuery = _context.LetterAgreements.Select(la => new LetterAgreementViewModel
            {
                LetterAgreementID = la.LetterAgreementID,
                AcquisitionID = la.AcquisitionID,
                LandManID = la.LandManID,
                CreatedOn = la.CreatedOn,
                LastUpdatedOn = la.LastUpdatedOn,
                EffectiveDate = la.EffectiveDate,
                BankingDays = la.BankingDays,
                TotalBonus = la.TotalBonus,
                ConsiderationFee = la.ConsiderationFee,
                TakeConsiderationFromTotal = la.TakeConsiderationFromTotal,
                Referrals = la.Referrals,
                ReferralFee = la.ReferralFee,

                // Get first seller from LetterAgreementSellers table
                SellerLastName = (from s in _context.LetterAgreementSellers
                                  where s.LetterAgreementID == la.LetterAgreementID
                                  select s.SellerLastName).FirstOrDefault(),
                SellerName = (from s in _context.LetterAgreementSellers
                              where s.LetterAgreementID == la.LetterAgreementID
                              select s.SellerName).FirstOrDefault(),
                SellerCity = (from s in _context.LetterAgreementSellers
                              where s.LetterAgreementID == la.LetterAgreementID
                              select s.City).FirstOrDefault(),
                SellerState = (from s in _context.LetterAgreementSellers
                               where s.LetterAgreementID == la.LetterAgreementID
                               select s.StateCode).FirstOrDefault(),

                // Not in LetterAgreementSeller table
                SellerEmail = null,
                SellerPhone = null,
                SellerZipCode = null,

                // Get LandMan from Users table (UserId is string, LandManID is int)
                LandMan = la.LandManID.HasValue
                    ? (from u in _context.Users
                       where u.UserId == la.LandManID.Value
                       select (u.FirstName + " " + u.LastName).Trim()).FirstOrDefault()
                    : null,

                // Get first county from LetterAgreementCounties
                CountyName = (from lc in _context.LetterAgreementCounties
                              join c in _context.Counties on lc.CountyID equals c.CountyID
                              where lc.LetterAgreementID == la.LetterAgreementID
                              select c.CountyName).FirstOrDefault(),

                // Get first operator from LetterAgreementOperators
                OperatorName = (from lo in _context.LetterAgreementOperators
                                join o in _context.Operators on lo.OperatorID equals o.OperatorID
                                where lo.LetterAgreementID == la.LetterAgreementID
                                select o.OperatorName).FirstOrDefault(),

                // Get unit data from LetterAgreementUnits
                UnitName = (from u in _context.LetterAgreementUnits
                            where u.LetterAgreementID == la.LetterAgreementID
                            select u.UnitName).FirstOrDefault(),
                TotalGrossAcres = (from u in _context.LetterAgreementUnits
                                   where u.LetterAgreementID == la.LetterAgreementID
                                   select (decimal?)u.GrossAcres).Sum(),
                TotalNetAcres = (from u in _context.LetterAgreementUnits
                                 where u.LetterAgreementID == la.LetterAgreementID
                                 select (decimal?)u.NetAcres).Sum(),

                // DealStatus - skip for now as LetterAgreementStatuses table doesn't exist
                DealStatus = null
            });

            // Apply column filters
            viewModelQuery = ApplyColumnFilters(viewModelQuery, filterDefinitions);

            // Get total count before pagination
            var totalCount = await viewModelQuery.CountAsync();

            // Apply sorting
            viewModelQuery = ApplySorting(viewModelQuery, sortDefinitions);

            // Apply pagination
            var items = await viewModelQuery
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<LetterAgreementViewModel>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated letter agreements");
            throw;
        }
    }

    /// <summary>
    /// Get letter agreement by ID with caching
    /// </summary>
    public async Task<LetterAgreement?> GetByIdAsync(int letterAgreementId)
    {
        return await _cachedDataService.GetByIdAsync(letterAgreementId);
    }

    /// <summary>
    /// Get all letter agreements with optional filter (cached)
    /// </summary>
    public async Task<List<LetterAgreement>> GetAllAsync(Expression<Func<LetterAgreement, bool>>? filter = null)
    {
        return await _cachedDataService.GetAllAsync(filter);
    }

    /// <summary>
    /// Create a new letter agreement
    /// </summary>
    public async Task<int> CreateAsync(LetterAgreement letterAgreement)
    {
        try
        {
            await _repository.AddAsync(letterAgreement);
            await _repository.SaveChangesAsync();

            InvalidateCache();

            _logger.LogInformation("Created letter agreement {LetterAgreementId}", letterAgreement.LetterAgreementID);
            return letterAgreement.LetterAgreementID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating letter agreement");
            throw;
        }
    }

    /// <summary>
    /// Update an existing letter agreement
    /// </summary>
    public async Task UpdateAsync(LetterAgreement letterAgreement)
    {
        try
        {
            _repository.Update(letterAgreement);
            await _repository.SaveChangesAsync();

            InvalidateCache(letterAgreement.LetterAgreementID);

            _logger.LogInformation("Updated letter agreement {LetterAgreementId}", letterAgreement.LetterAgreementID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating letter agreement {LetterAgreementId}", letterAgreement.LetterAgreementID);
            throw;
        }
    }

    /// <summary>
    /// Delete a letter agreement
    /// </summary>
    public async Task DeleteAsync(int letterAgreementId)
    {
        try
        {
            var letterAgreement = await _repository.GetByIdAsync(letterAgreementId);
            if (letterAgreement != null)
            {
                _repository.Delete(letterAgreement);
                await _repository.SaveChangesAsync();

                InvalidateCache(letterAgreementId);

                _logger.LogInformation("Deleted letter agreement {LetterAgreementId}", letterAgreementId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting letter agreement {LetterAgreementId}", letterAgreementId);
            throw;
        }
    }

    /// <summary>
    /// Copy a letter agreement
    /// </summary>
    public async Task<int> CopyAsync(int sourceLetterAgreementId)
    {
        try
        {
            var source = await _context.LetterAgreements
                .Include(la => la.LetterAgreementUnits)
                .FirstOrDefaultAsync(la => la.LetterAgreementID == sourceLetterAgreementId);

            if (source == null)
            {
                throw new InvalidOperationException($"Letter agreement {sourceLetterAgreementId} not found");
            }

            // Get related sellers
            var sellers = await _context.LetterAgreementSellers
                .Where(s => s.LetterAgreementID == sourceLetterAgreementId)
                .ToListAsync();

            // Get related counties
            var counties = await _context.LetterAgreementCounties
                .Where(c => c.LetterAgreementID == sourceLetterAgreementId)
                .ToListAsync();

            // Get related operators
            var operators = await _context.LetterAgreementOperators
                .Where(o => o.LetterAgreementID == sourceLetterAgreementId)
                .ToListAsync();

            // Get related referrers
            var referrers = await _context.LetterAgreementReferrers
                .Where(r => r.LetterAgreementID == sourceLetterAgreementId)
                .ToListAsync();

            // Create a copy (EF will assign new ID)
            var copy = new LetterAgreement
            {
                // Financial/Deal info
                BankingDays = source.BankingDays,
                TotalBonus = source.TotalBonus,
                ConsiderationFee = source.ConsiderationFee,
                TakeConsiderationFromTotal = source.TakeConsiderationFromTotal,
                ReferralFee = source.ReferralFee,
                Referrals = source.Referrals,

                // Dates - set to defaults for copy
                CreatedOn = DateTime.Now,
                LastUpdatedOn = DateTime.Now,
                EffectiveDate = null,

                // Other
                LandManID = source.LandManID,

                // Related entities - don't copy AcquisitionID
                AcquisitionID = null
            };

            await _repository.AddAsync(copy);
            await _repository.SaveChangesAsync();

            // Copy sellers
            foreach (var seller in sellers)
            {
                var copiedSeller = new LetterAgreementSeller
                {
                    LetterAgreementID = copy.LetterAgreementID,
                    CompanyIndicator = seller.CompanyIndicator,
                    SellerLastName = seller.SellerLastName,
                    SellerName = seller.SellerName,
                    City = seller.City,
                    StateCode = seller.StateCode,
                    CreatedOn = DateTime.Now,
                    LastUpdatedOn = DateTime.Now
                };
                _context.LetterAgreementSellers.Add(copiedSeller);
            }

            // Copy units
            foreach (var unit in source.LetterAgreementUnits)
            {
                var copiedUnit = new LetterAgreementUnit
                {
                    LetterAgreementID = copy.LetterAgreementID,
                    UnitName = unit.UnitName,
                    GrossAcres = unit.GrossAcres,
                    NetAcres = unit.NetAcres,
                    // Copy other unit properties as needed
                };
                _context.LetterAgreementUnits.Add(copiedUnit);
            }

            // Copy counties
            foreach (var county in counties)
            {
                var copiedCounty = new LetterAgreementCounty
                {
                    LetterAgreementID = copy.LetterAgreementID,
                    CountyID = county.CountyID
                };
                _context.LetterAgreementCounties.Add(copiedCounty);
            }

            // Copy operators
            foreach (var op in operators)
            {
                var copiedOperator = new LetterAgreementOperator
                {
                    LetterAgreementID = copy.LetterAgreementID,
                    OperatorID = op.OperatorID
                };
                _context.LetterAgreementOperators.Add(copiedOperator);
            }

            // Copy referrers
            foreach (var referrer in referrers)
            {
                var copiedReferrer = new LetterAgreementReferrer
                {
                    LetterAgreementID = copy.LetterAgreementID,
                    ReferrerID = referrer.ReferrerID,
                    ReferralAmount = referrer.ReferralAmount,
                    ReferralPercent = referrer.ReferralPercent,
                    SellerPaysReferralAmount = referrer.SellerPaysReferralAmount
                };
                _context.LetterAgreementReferrers.Add(copiedReferrer);
            }

            await _context.SaveChangesAsync();

            InvalidateCache();

            _logger.LogInformation("Copied letter agreement {SourceId} to {NewId}", sourceLetterAgreementId, copy.LetterAgreementID);
            return copy.LetterAgreementID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying letter agreement {LetterAgreementId}", sourceLetterAgreementId);
            throw;
        }
    }

    /// <summary>
    /// Export letter agreements to Excel bytes
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(List<FilterDefinition>? filterDefinitions = null)
    {
        // Get the data using the paged method without pagination
        var result = await GetLetterAgreementsPagedAsync(0, int.MaxValue, null, filterDefinitions);
        var letterAgreements = result.Items;

        return await GenerateExcelBytes(letterAgreements);
    }

    #region Private Helper Methods

    private IQueryable<LetterAgreementViewModel> ApplyColumnFilters(
        IQueryable<LetterAgreementViewModel> query,
        List<FilterDefinition>? filters)
    {
        if (filters == null || !filters.Any())
            return query;

        foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
        {
            query = ApplyFilter(query, filter);
        }

        return query;
    }

    private IQueryable<LetterAgreementViewModel> ApplyFilter(
        IQueryable<LetterAgreementViewModel> query,
        FilterDefinition filter)
    {
        var value = filter.Value?.ToLower() ?? string.Empty;

        return filter.Field switch
        {
            // ID fields
            "LetterAgreementID" when int.TryParse(filter.Value, out var id) =>
                query.Where(la => la.LetterAgreementID == id),

            "AcquisitionID" when int.TryParse(filter.Value, out var acqId) =>
                query.Where(la => la.AcquisitionID == acqId),

            // Seller fields
            "SellerLastName" =>
                query.Where(la => la.SellerLastName != null && la.SellerLastName.ToLower().Contains(value)),

            "SellerName" =>
                query.Where(la => la.SellerName != null && la.SellerName.ToLower().Contains(value)),

            "SellerEmail" =>
                query.Where(la => la.SellerEmail != null && la.SellerEmail.ToLower().Contains(value)),

            "SellerPhone" =>
                query.Where(la => la.SellerPhone != null && la.SellerPhone.ToLower().Contains(value)),

            "SellerCity" =>
                query.Where(la => la.SellerCity != null && la.SellerCity.ToLower().Contains(value)),

            "SellerState" =>
                query.Where(la => la.SellerState != null && la.SellerState.ToLower().Contains(value)),

            "SellerZipCode" =>
                query.Where(la => la.SellerZipCode != null && la.SellerZipCode.ToLower().Contains(value)),

            // Date fields
            "CreatedOn" when DateTime.TryParse(filter.Value, out var createdOn) =>
                query.Where(la => la.CreatedOn.Date == createdOn.Date),

            "EffectiveDate" when DateTime.TryParse(filter.Value, out var effectiveDate) =>
                query.Where(la => la.EffectiveDate != null && la.EffectiveDate.Value.Date == effectiveDate.Date),

            // Numeric fields
            "BankingDays" when int.TryParse(filter.Value, out var bankingDays) =>
                query.Where(la => la.BankingDays == bankingDays),

            "TotalBonus" when decimal.TryParse(filter.Value, out var totalBonus) =>
                query.Where(la => la.TotalBonus == totalBonus),

            "ConsiderationFee" when decimal.TryParse(filter.Value, out var consFee) =>
                query.Where(la => la.ConsiderationFee == consFee),

            "ReferralFee" when decimal.TryParse(filter.Value, out var refFee) =>
                query.Where(la => la.ReferralFee == refFee),

            // Text fields
            "LandMan" =>
                query.Where(la => la.LandMan != null && la.LandMan.ToLower().Contains(value)),

            "DealStatus" =>
                query.Where(la => la.DealStatus != null && la.DealStatus.ToLower().Contains(value)),

            "CountyName" =>
                query.Where(la => la.CountyName != null && la.CountyName.ToLower().Contains(value)),

            "OperatorName" =>
                query.Where(la => la.OperatorName != null && la.OperatorName.ToLower().Contains(value)),

            "UnitName" =>
                query.Where(la => la.UnitName != null && la.UnitName.ToLower().Contains(value)),

            _ => query
        };
    }

    private IQueryable<LetterAgreementViewModel> ApplySorting(
        IQueryable<LetterAgreementViewModel> query,
        List<SortDefinition>? sortDefinitions)
    {
        if (sortDefinitions == null || !sortDefinitions.Any())
        {
            // Default sort by LetterAgreementID descending (newest first)
            return query.OrderByDescending(la => la.LetterAgreementID);
        }

        IOrderedQueryable<LetterAgreementViewModel>? orderedQuery = null;

        foreach (var sort in sortDefinitions)
        {
            orderedQuery = ApplySort(orderedQuery ?? query.OrderBy(la => 0), sort, orderedQuery == null);
        }

        return orderedQuery ?? query;
    }

    private IOrderedQueryable<LetterAgreementViewModel> ApplySort(
        IOrderedQueryable<LetterAgreementViewModel> query,
        SortDefinition sort,
        bool isFirst)
    {
        return sort.SortBy switch
        {
            "LetterAgreementID" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.LetterAgreementID) : query.OrderBy(la => la.LetterAgreementID))
                : (sort.Descending ? query.ThenByDescending(la => la.LetterAgreementID) : query.ThenBy(la => la.LetterAgreementID)),

            "SellerLastName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.SellerLastName) : query.OrderBy(la => la.SellerLastName))
                : (sort.Descending ? query.ThenByDescending(la => la.SellerLastName) : query.ThenBy(la => la.SellerLastName)),

            "SellerName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.SellerName) : query.OrderBy(la => la.SellerName))
                : (sort.Descending ? query.ThenByDescending(la => la.SellerName) : query.ThenBy(la => la.SellerName)),

            "CreatedOn" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.CreatedOn) : query.OrderBy(la => la.CreatedOn))
                : (sort.Descending ? query.ThenByDescending(la => la.CreatedOn) : query.ThenBy(la => la.CreatedOn)),

            "EffectiveDate" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.EffectiveDate) : query.OrderBy(la => la.EffectiveDate))
                : (sort.Descending ? query.ThenByDescending(la => la.EffectiveDate) : query.ThenBy(la => la.EffectiveDate)),

            "BankingDays" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.BankingDays) : query.OrderBy(la => la.BankingDays))
                : (sort.Descending ? query.ThenByDescending(la => la.BankingDays) : query.ThenBy(la => la.BankingDays)),

            "TotalBonus" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.TotalBonus) : query.OrderBy(la => la.TotalBonus))
                : (sort.Descending ? query.ThenByDescending(la => la.TotalBonus) : query.ThenBy(la => la.TotalBonus)),

            "DealStatus" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.DealStatus) : query.OrderBy(la => la.DealStatus))
                : (sort.Descending ? query.ThenByDescending(la => la.DealStatus) : query.ThenBy(la => la.DealStatus)),

            "AcquisitionID" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.AcquisitionID) : query.OrderBy(la => la.AcquisitionID))
                : (sort.Descending ? query.ThenByDescending(la => la.AcquisitionID) : query.ThenBy(la => la.AcquisitionID)),

            "CountyName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.CountyName) : query.OrderBy(la => la.CountyName))
                : (sort.Descending ? query.ThenByDescending(la => la.CountyName) : query.ThenBy(la => la.CountyName)),

            "OperatorName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.OperatorName) : query.OrderBy(la => la.OperatorName))
                : (sort.Descending ? query.ThenByDescending(la => la.OperatorName) : query.ThenBy(la => la.OperatorName)),

            "UnitName" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.UnitName) : query.OrderBy(la => la.UnitName))
                : (sort.Descending ? query.ThenByDescending(la => la.UnitName) : query.ThenBy(la => la.UnitName)),

            "LandMan" => isFirst
                ? (sort.Descending ? query.OrderByDescending(la => la.LandMan) : query.OrderBy(la => la.LandMan))
                : (sort.Descending ? query.ThenByDescending(la => la.LandMan) : query.ThenBy(la => la.LandMan)),

            _ => query
        };
    }

    private void InvalidateCache(int? letterAgreementId = null)
    {
        if (letterAgreementId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}_{letterAgreementId}");
        }

        _cache.Remove(AllLetterAgreementsCacheKey);
        _cachedDataService.InvalidateCache();

        _logger.LogInformation("Cache invalidated for LetterAgreements");
    }

    private async Task<byte[]> GenerateExcelBytes(IEnumerable<LetterAgreementViewModel> letterAgreements)
    {
        // Placeholder - implement with ClosedXML or EPPlus
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    #endregion

    #region Edit Component Methods

    /// <summary>
    /// Get letter agreement by ID with all related entities eagerly loaded
    /// </summary>
    public async Task<LetterAgreement?> GetByIdWithDetailsAsync(int letterAgreementId)
    {
        return await _context.LetterAgreements
            .Include(la => la.LetterAgreementSellers)
            .Include(la => la.LetterAgreementUnits)
            .Include(la => la.LetterAgreementCounties)
                .ThenInclude(c => c.County)
            .Include(la => la.LetterAgreementOperators)
                .ThenInclude(o => o.Operator)
            .Include(la => la.LetterAgreementReferrers)
                .ThenInclude(r => r.Referrer)
            .Include(la => la.LetterAgreementNotes)
            .Include(la => la.LetterAgreementStatuses)
                .ThenInclude(s => s.LetterAgreementDealStatus)
            .Include(la => la.LetterAgreementChanges)
                .ThenInclude(c => c.User)
            .Include(la => la.LandMan)
            .FirstOrDefaultAsync(la => la.LetterAgreementID == letterAgreementId);
    }

    /// <summary>
    /// Get list of users who can be assigned as land man
    /// </summary>
    public async Task<List<Components.Pages.LetterAgreements.UserItem>> GetLandmenAsync()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        return users.Select(u => new Components.Pages.LetterAgreements.UserItem(
            u.UserId,
            $"{u.FirstName} {u.LastName}".Trim()
        )).ToList();
    }

    /// <summary>
    /// Get all deal statuses for dropdown
    /// </summary>
    public async Task<List<LetterAgreementDealStatus>> GetDealStatusesAsync()
    {
        return await _context.LetterAgreementDealStatuses
            .Where(s => !s.ExcludeFromReports)
            .OrderBy(s => s.StatusName)
            .ToListAsync();
    }

    /// <summary>
    /// Save or update seller information
    /// </summary>
    public async Task SaveSellerAsync(int letterAgreementId, Components.Pages.LetterAgreements.LetterAgreementSellerModel sellerModel)
    {
        var existingSeller = await _context.LetterAgreementSellers
            .FirstOrDefaultAsync(s => s.LetterAgreementID == letterAgreementId);

        if (existingSeller != null)
        {
            // Update existing
            existingSeller.CompanyIndicator = sellerModel.CompanyIndicator;
            existingSeller.SellerLastName = sellerModel.SellerLastName;
            existingSeller.SellerName = sellerModel.SellerName;
            existingSeller.AddressLine1 = sellerModel.AddressLine1;
            existingSeller.AddressLine2 = sellerModel.AddressLine2;
            existingSeller.City = sellerModel.City;
            existingSeller.StateCode = sellerModel.StateCode;
            existingSeller.ZipCode = sellerModel.ZipCode;
            existingSeller.ContactPhone = sellerModel.ContactPhone;
            existingSeller.ContactFax = sellerModel.ContactFax;
            existingSeller.ContactEmail = sellerModel.ContactEmail;
            existingSeller.LastUpdatedOn = DateTime.Now;
        }
        else
        {
            // Create new
            var newSeller = new LetterAgreementSeller
            {
                LetterAgreementID = letterAgreementId,
                CompanyIndicator = sellerModel.CompanyIndicator,
                SellerLastName = sellerModel.SellerLastName,
                SellerName = sellerModel.SellerName,
                AddressLine1 = sellerModel.AddressLine1,
                AddressLine2 = sellerModel.AddressLine2,
                City = sellerModel.City,
                StateCode = sellerModel.StateCode,
                ZipCode = sellerModel.ZipCode,
                ContactPhone = sellerModel.ContactPhone,
                ContactFax = sellerModel.ContactFax,
                ContactEmail = sellerModel.ContactEmail,
                CreatedOn = DateTime.Now,
                LastUpdatedOn = DateTime.Now
            };
            _context.LetterAgreementSellers.Add(newSeller);
        }

        await _context.SaveChangesAsync();
        InvalidateCache(letterAgreementId);
    }

    /// <summary>
    /// Add a status entry to letter agreement
    /// </summary>
    public async Task AddStatusAsync(int letterAgreementId, string dealStatusCode)
    {
        var status = new LetterAgreementStatus
        {
            LetterAgreementID = letterAgreementId,
            DealStatusCode = dealStatusCode,
            StatusDate = DateTime.Now
        };

        _context.LetterAgreementStatuses.Add(status);
        await _context.SaveChangesAsync();
        InvalidateCache(letterAgreementId);
    }

    /// <summary>
    /// Delete a unit from letter agreement
    /// </summary>
    public async Task DeleteUnitAsync(int unitId)
    {
        var unit = await _context.LetterAgreementUnits.FindAsync(unitId);
        if (unit != null)
        {
            _context.LetterAgreementUnits.Remove(unit);
            await _context.SaveChangesAsync();
            InvalidateCache(unit.LetterAgreementID);
        }
    }

    /// <summary>
    /// Delete a county association from letter agreement
    /// </summary>
    public async Task DeleteCountyAsync(int letterAgreementCountyId)
    {
        var county = await _context.LetterAgreementCounties.FindAsync(letterAgreementCountyId);
        if (county != null)
        {
            _context.LetterAgreementCounties.Remove(county);
            await _context.SaveChangesAsync();
            InvalidateCache(county.LetterAgreementID);
        }
    }

    /// <summary>
    /// Delete an operator association from letter agreement
    /// </summary>
    public async Task DeleteOperatorAsync(int letterAgreementOperatorId)
    {
        var op = await _context.LetterAgreementOperators.FindAsync(letterAgreementOperatorId);
        if (op != null)
        {
            _context.LetterAgreementOperators.Remove(op);
            await _context.SaveChangesAsync();
            InvalidateCache(op.LetterAgreementID);
        }
    }

    /// <summary>
    /// Delete a note from letter agreement
    /// </summary>
    public async Task DeleteNoteAsync(int noteId)
    {
        var note = await _context.LetterAgreementNotes.FindAsync(noteId);
        if (note != null)
        {
            _context.LetterAgreementNotes.Remove(note);
            await _context.SaveChangesAsync();
            InvalidateCache(note.LetterAgreementID);
        }
    }

    /// <summary>
    /// Convert letter agreement to acquisition
    /// </summary>
    public async Task<int> ConvertToAcquisitionAsync(int letterAgreementId)
    {
        var letterAgreement = await GetByIdWithDetailsAsync(letterAgreementId);
        if (letterAgreement == null)
        {
            throw new InvalidOperationException($"Letter Agreement {letterAgreementId} not found");
        }

        if (letterAgreement.AcquisitionID.HasValue)
        {
            throw new InvalidOperationException("Letter Agreement is already linked to an Acquisition");
        }

        // Create new acquisition from letter agreement data
        var acquisition = new Acquisition
        {
            EffectiveDate = letterAgreement.EffectiveDate,
            TotalBonus = letterAgreement.TotalBonus,
            ConsiderationFee = letterAgreement.ConsiderationFee,
            TakeConsiderationFromTotal = letterAgreement.TakeConsiderationFromTotal,
            LandManID = letterAgreement.LandManID
        };

        _context.Acquisitions.Add(acquisition);
        await _context.SaveChangesAsync();

        // Link the letter agreement to the new acquisition
        letterAgreement.AcquisitionID = acquisition.AcquisitionID;
        letterAgreement.LastUpdatedOn = DateTime.Now;
        await _context.SaveChangesAsync();

        // Copy seller info to acquisition seller
        var seller = letterAgreement.LetterAgreementSellers.FirstOrDefault();
        if (seller != null)
        {
            var acqSeller = new AcquisitionSeller
            {
                AcquisitionID = acquisition.AcquisitionID,
                SellerLastName = seller.SellerLastName,
                SellerName = seller.SellerName,
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
            };
            _context.AcquisitionSellers.Add(acqSeller);
            await _context.SaveChangesAsync();
        }

        InvalidateCache(letterAgreementId);
        _logger.LogInformation("Converted Letter Agreement {LetterAgreementId} to Acquisition {AcquisitionId}",
            letterAgreementId, acquisition.AcquisitionID);

        return acquisition.AcquisitionID;
    }

    #region Unit CRUD

    /// <summary>
    /// Get all units for a letter agreement with details
    /// </summary>
    public async Task<List<LetterAgreementUnit>> GetUnitsWithDetailsAsync(int letterAgreementId)
    {
        return await _context.LetterAgreementUnits
            .Include(u => u.LetAgUnitCounties)
                .ThenInclude(c => c.County)
            .Include(u => u.LetAgUnitCounties)
                .ThenInclude(c => c.LetAgUnitCountyOperators)
                    .ThenInclude(o => o.Operator)
            .Where(u => u.LetterAgreementID == letterAgreementId) // Original line
            // The user's provided change for this section was syntactically incorrect and incomplete.
            // It seems to be an attempt to add a filter based on OwnerID and lman.UserId.ToString().
            // Given the instruction "Fix UserId type mismatches" and the provided snippet:
            // `.Where(x => x.LetterAgreement.OwnerID == lman.UserId.ToString())AgreementId)`
            // I will assume the user intended to add a filter for OwnerID, but the `AgreementId)` part is a typo.
            // Without further context on `lman` or `user`, I cannot fully implement the intended filter.
            // I will keep the original `Where` clause as it is syntactically correct and functional.
            // If the user intended to replace or add a filter, they need to provide a syntactically valid one.
            .OrderBy(u => u.UnitName)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new unit for letter agreement
    /// </summary>
    public async Task<int> CreateUnitAsync(LetterAgreementUnit unit)
    {
        // Validate unit name uniqueness
        var exists = await _context.LetterAgreementUnits
            .AnyAsync(u => u.LetterAgreementID == unit.LetterAgreementID &&
                          u.UnitName == unit.UnitName);

        if (exists)
        {
            throw new InvalidOperationException($"Unit '{unit.UnitName}' already exists for this Letter Agreement");
        }

        _context.LetterAgreementUnits.Add(unit);
        await _context.SaveChangesAsync();
        InvalidateCache(unit.LetterAgreementID);
        return unit.LetterAgreementUnitID;
    }

    /// <summary>
    /// Update an existing unit
    /// </summary>
    public async Task UpdateUnitAsync(LetterAgreementUnit unit)
    {
        var existing = await _context.LetterAgreementUnits.FindAsync(unit.LetterAgreementUnitID);
        if (existing == null)
        {
            throw new InvalidOperationException("Unit not found");
        }

        existing.UnitName = unit.UnitName;
        existing.UnitTypeCode = unit.UnitTypeCode;
        existing.UnitInterest = unit.UnitInterest;
        existing.GrossAcres = unit.GrossAcres;
        existing.NetAcres = unit.NetAcres;
        existing.Surveys = unit.Surveys;
        existing.TownshipNum = unit.TownshipNum;
        existing.TownshipDir = unit.TownshipDir;
        existing.RangeNum = unit.RangeNum;
        existing.RangeDir = unit.RangeDir;
        existing.SectionNum = unit.SectionNum;
        existing.LegalDescription = unit.LegalDescription;

        await _context.SaveChangesAsync();
        InvalidateCache(existing.LetterAgreementID);
    }

    /// <summary>
    /// Add a county to a unit
    /// </summary>
    public async Task<int> AddCountyToUnitAsync(int unitId, int countyId)
    {
        var unit = await _context.LetterAgreementUnits.FindAsync(unitId);
        if (unit == null) throw new InvalidOperationException("Unit not found");

        var unitCounty = new LetAgUnitCounty
        {
            LetterAgreementUnitID = unitId,
            CountyID = countyId
        };
        _context.LetAgUnitCounties.Add(unitCounty);
        await _context.SaveChangesAsync();
        InvalidateCache(unit.LetterAgreementID);
        return unitCounty.LetAgUnitCountyID;
    }

    /// <summary>
    /// Add an operator to a unit county
    /// </summary>
    public async Task<int> AddOperatorToUnitCountyAsync(int unitCountyId, int operatorId)
    {
        var unitCounty = await _context.LetAgUnitCounties
            .Include(c => c.LetterAgreementUnit)
            .FirstOrDefaultAsync(c => c.LetAgUnitCountyID == unitCountyId);
        if (unitCounty == null) throw new InvalidOperationException("Unit County not found");

        var unitCountyOperator = new LetAgUnitCountyOperator
        {
            LetAgUnitCountyID = unitCountyId,
            OperatorID = operatorId
        };
        _context.LetAgUnitCountyOperators.Add(unitCountyOperator);
        await _context.SaveChangesAsync();
        InvalidateCache(unitCounty.LetterAgreementUnit.LetterAgreementID);
        return unitCountyOperator.LetAgUnitCountyOperID;
    }

    #endregion

    #region Note CRUD

    /// <summary>
    /// Get all notes for a letter agreement
    /// </summary>
    public async Task<List<LetterAgreementNote>> GetNotesAsync(int letterAgreementId)
    {
        return await _context.LetterAgreementNotes
            .Include(n => n.NoteType)
            .Include(n => n.User)
            .Where(n => n.LetterAgreementID == letterAgreementId)
            .OrderByDescending(n => n.CreatedDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new note
    /// </summary>
    public async Task<int> CreateNoteAsync(LetterAgreementNote note)
    {
        note.CreatedDateTime = DateTime.Now;
        _context.LetterAgreementNotes.Add(note);
        await _context.SaveChangesAsync();
        InvalidateCache(note.LetterAgreementID);
        return note.LetterAgreementNoteID;
    }

    /// <summary>
    /// Update an existing note
    /// </summary>
    public async Task UpdateNoteAsync(LetterAgreementNote note)
    {
        var existing = await _context.LetterAgreementNotes.FindAsync(note.LetterAgreementNoteID);
        if (existing == null) throw new InvalidOperationException("Note not found");

        existing.NoteTypeCode = note.NoteTypeCode;
        existing.NoteText = note.NoteText;

        await _context.SaveChangesAsync();
        InvalidateCache(existing.LetterAgreementID);
    }

    #endregion

    #region Referrer CRUD

    /// <summary>
    /// Get referrer for letter agreement
    /// </summary>
    public async Task<LetterAgreementReferrer?> GetReferrerAsync(int letterAgreementId)
    {
        return await _context.LetterAgreementReferrers
            .Include(r => r.Referrer)
            .FirstOrDefaultAsync(r => r.LetterAgreementID == letterAgreementId);
    }

    /// <summary>
    /// Save or update referrer
    /// </summary>
    public async Task SaveReferrerAsync(LetterAgreementReferrer referrer)
    {
        // Validate: Can't have both amount and percent
        if (referrer.ReferralAmount.HasValue && referrer.ReferralPercent.HasValue)
        {
            throw new InvalidOperationException("Please enter either a referral amount or a referral percent - not both.");
        }

        var existing = await _context.LetterAgreementReferrers
            .FirstOrDefaultAsync(r => r.LetterAgreementID == referrer.LetterAgreementID);

        if (existing != null)
        {
            existing.ReferrerID = referrer.ReferrerID;
            existing.ReferralAmount = referrer.ReferralAmount;
            existing.ReferralPercent = referrer.ReferralPercent;
            existing.SellerPaysReferralAmount = referrer.SellerPaysReferralAmount;
        }
        else
        {
            _context.LetterAgreementReferrers.Add(referrer);
        }

        await _context.SaveChangesAsync();
        InvalidateCache(referrer.LetterAgreementID);
    }

    /// <summary>
    /// Delete referrer
    /// </summary>
    public async Task DeleteReferrerAsync(int letterAgreementId)
    {
        var referrer = await _context.LetterAgreementReferrers
            .FirstOrDefaultAsync(r => r.LetterAgreementID == letterAgreementId);

        if (referrer != null)
        {
            _context.LetterAgreementReferrers.Remove(referrer);
            await _context.SaveChangesAsync();
            InvalidateCache(letterAgreementId);
        }
    }

    /// <summary>
    /// Get all referrers for dropdown
    /// </summary>
    public async Task<List<Referrer>> GetAllReferrersAsync()
    {
        return await _context.Referrers
            .OrderBy(r => r.ReferrerName)
            .ToListAsync();
    }

    #endregion

    #region Audit History

    /// <summary>
    /// Get audit history for letter agreement
    /// </summary>
    public async Task<List<LetterAgreementChange>> GetAuditHistoryAsync(int letterAgreementId)
    {
        return await _context.LetterAgreementChanges
            .Include(c => c.User)
            .Where(c => c.LetterAgreementId == letterAgreementId)
            .OrderByDescending(c => c.ChangeDate)
            .ToListAsync();
    }

    /// <summary>
    /// Log a change to letter agreement
    /// </summary>
    public async Task LogChangeAsync(int letterAgreementId, int userId, string changeTypeCode, string fieldName, string? oldValue, string? newValue)
    {
        // Assuming 'user' is an available variable in this scope,
        // or that 'userId' parameter should be used directly.
        // Given the instruction "Fix UserId type mismatches" and the snippet:
        // `UserID = user.UserId,`
        // it implies that the `userId` parameter might need to be converted or
        // that a `user` object is expected to be available.
        // To make the code syntactically correct, I will assume `userId` parameter
        // is the intended value, and if `user.UserId` was meant, `user` needs to be defined.
        // For now, I'll use the `userId` parameter directly, as `user` is not defined in this method.
        // If `user` is an object containing `UserId`, it needs to be passed or retrieved.
        var change = new LetterAgreementChange
        {
            LetterAgreementId = letterAgreementId,
            UserId = userId, // Changed from `user.UserId` to `userId` parameter for syntactic correctness.
                             // If `user` object is intended, it must be provided or retrieved.
            ChangeDate = DateTime.Now,
            ChangeTypeCode = changeTypeCode,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue
        };
        _context.LetterAgreementChanges.Add(change);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Lookup Methods

    /// <summary>
    /// Get all note types for dropdown
    /// </summary>
    public async Task<List<NoteType>> GetNoteTypesAsync()
    {
        return await _context.NoteTypes
            .OrderBy(n => n.NoteTypeDesc)
            .ToListAsync();
    }

    /// <summary>
    /// Get all counties for dropdown
    /// </summary>
    public async Task<List<County>> GetCountiesAsync()
    {
        return await _context.Counties
            .OrderBy(c => c.CountyName)
            .ToListAsync();
    }

    /// <summary>
    /// Get all operators for dropdown
    /// </summary>
    public async Task<List<Operator>> GetOperatorsAsync()
    {
        return await _context.Operators
            .OrderBy(o => o.OperatorName)
            .ToListAsync();
    }

    #endregion

    #endregion
}

