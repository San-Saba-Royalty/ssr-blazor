using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for DocumentTemplate operations with caching support
/// </summary>
public class DocumentService
{
    private readonly IDbContextFactory<SsrDbContext> _contextFactory;
    private readonly CachedDataService<DocumentTemplate> _cachedDataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DocumentService> _logger;

    private const string CacheKeyPrefix = "DocumentTemplate";
    private const string AllDocumentsCacheKey = $"{CacheKeyPrefix}_All";
    private const string DocumentTypesCacheKey = $"{CacheKeyPrefix}_Types";

    public DocumentService(
        IDbContextFactory<SsrDbContext> contextFactory,
        CachedDataService<DocumentTemplate> cachedDataService,
        IMemoryCache cache,
        ILogger<DocumentService> logger)
    {
        _contextFactory = contextFactory;
        _cachedDataService = cachedDataService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated document templates with filtering and sorting
    /// </summary>
    public async Task<PagedResult<DocumentTemplate>> GetDocumentTemplatesPagedAsync(
        int page,
        int pageSize,
        string? documentTypeCode = null,
        List<SortDefinition>? sortDefinitions = null,
        List<FilterDefinition>? filterDefinitions = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var repository = new DocumentTemplateRepository(context);
            var query = await repository.GetDocumentTemplatesAsync();

            // Apply document type filter
            if (!string.IsNullOrEmpty(documentTypeCode))
            {
                query = query.Where(d => d.DocumentTypeCode == documentTypeCode);
            }

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

            return new PagedResult<DocumentTemplate>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated document templates");
            throw;
        }
    }

    /// <summary>
    /// Get document template by ID with caching
    /// </summary>
    public async Task<DocumentTemplate?> GetByIdAsync(int documentTemplateId)
    {
        return await _cachedDataService.GetByIdAsync(documentTemplateId);
    }

    /// <summary>
    /// Get all document templates with optional filter (cached)
    /// </summary>
    public async Task<List<DocumentTemplate>> GetAllAsync(Expression<Func<DocumentTemplate, bool>>? filter = null)
    {
        return await _cachedDataService.GetAllAsync(filter);
    }

    /// <summary>
    /// Get available document types for dropdown
    /// </summary>
    public async Task<List<DocumentTypeItem>> GetDocumentTypesAsync()
    {
        return await _cache.GetOrCreateAsync(DocumentTypesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            await using var context = await _contextFactory.CreateDbContextAsync();
            var repository = new DocumentTemplateRepository(context);
            var query = await repository.GetDocumentTemplatesAsync();

            var types = await query
                .Select(d => d.DocumentTypeCode)
                .Where(code => !string.IsNullOrEmpty(code))
                .Distinct()
                .OrderBy(code => code)
                .ToListAsync();

            var result = new List<DocumentTypeItem>
            {
                new DocumentTypeItem { Code = "", Name = "All Document Types" }
            };

            result.AddRange(types.Select(t => new DocumentTypeItem
            {
                Code = t!,
                Name = t!
            }));

            return result;
        }) ?? new List<DocumentTypeItem>();
    }

    /// <summary>
    /// Create a new document template
    /// </summary>
    public async Task<int> CreateAsync(DocumentTemplate documentTemplate)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var repository = new DocumentTemplateRepository(context);
            await repository.AddAsync(documentTemplate);
            await repository.SaveChangesAsync();

            InvalidateCache();

            _logger.LogInformation("Created document template {DocumentTemplateId}", documentTemplate.DocumentTemplateID);
            return documentTemplate.DocumentTemplateID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document template");
            throw;
        }
    }

    /// <summary>
    /// Update an existing document template
    /// </summary>
    public async Task UpdateAsync(DocumentTemplate documentTemplate)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var repository = new DocumentTemplateRepository(context);

            // Handle CustomFields: Delete existing and add new
            // Note: This relies on the context tracking or explicitly loading them. 
            // Since we use a repository pattern and disconnected entities (likely coming from Blazor), 
            // we need to be careful.

            // Get existing entity with tracked custom fields
            var existing = await repository.GetByIdAsync(documentTemplate.DocumentTemplateID);
            if (existing != null)
            {
                // Update scalar properties
                context.Entry(existing).CurrentValues.SetValues(documentTemplate);

                // Update Custom Fields
                // Clear existing
                existing.CustomFields.Clear();

                // Add new
                foreach (var cf in documentTemplate.CustomFields)
                {
                    existing.CustomFields.Add(cf);
                }

                await repository.SaveChangesAsync();
                InvalidateCache(documentTemplate.DocumentTemplateID);
                _logger.LogInformation("Updated document template {DocumentTemplateId} and custom fields", documentTemplate.DocumentTemplateID);
            }
            else
            {
                // Fallback if not found, though realistically it should exist
                repository.Update(documentTemplate);
                await repository.SaveChangesAsync();
                InvalidateCache(documentTemplate.DocumentTemplateID);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document template {DocumentTemplateId}", documentTemplate.DocumentTemplateID);
            throw;
        }
    }

    /// <summary>
    /// Delete a document template
    /// </summary>
    public async Task DeleteAsync(int documentTemplateId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var repository = new DocumentTemplateRepository(context);
            var documentTemplate = await repository.GetByIdAsync(documentTemplateId);
            if (documentTemplate != null)
            {
                repository.Delete(documentTemplate);
                await repository.SaveChangesAsync();

                InvalidateCache(documentTemplateId);

                _logger.LogInformation("Deleted document template {DocumentTemplateId}", documentTemplateId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document template {DocumentTemplateId}", documentTemplateId);
            throw;
        }
    }

    /// <summary>
    /// Check if document template has associated file
    /// </summary>
    public async Task<bool> HasAssociatedDocumentsAsync(int documentTemplateId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var repository = new DocumentTemplateRepository(context);
        // Check if there is a file location or DocuSign file ID
        var template = await repository.GetByIdAsync(documentTemplateId);
        return template != null &&
               (!string.IsNullOrEmpty(template.DocumentTemplateLocation) ||
                !string.IsNullOrEmpty(template.DSFileID));
    }

    /// <summary>
    /// Check if a template description already exists for the given document type
    /// </summary>
    /// <param name="documentTypeCode">Document type code</param>
    /// <param name="description">Description to check</param>
    /// <param name="excludeId">Template ID to exclude (for edit scenarios)</param>
    /// <returns>True if description exists</returns>
    public async Task<bool> DoesTemplateDescriptionExistAsync(string documentTypeCode, string description, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var repository = new DocumentTemplateRepository(context);
        var query = repository.Query()
            .Where(t => t.DocumentTypeCode == documentTypeCode &&
                        t.DocumentTemplateDesc == description);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.DocumentTemplateID != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Check if a template file already exists for the given document type
    /// </summary>
    /// <param name="documentTypeCode">Document type code</param>
    /// <param name="fileName">File name to check</param>
    /// <param name="excludeId">Template ID to exclude (for edit scenarios)</param>
    /// <returns>True if file exists</returns>
    public async Task<bool> DoesTemplateFileExistAsync(string documentTypeCode, string fileName, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var repository = new DocumentTemplateRepository(context);
        var query = repository.Query()
            .Where(t => t.DocumentTypeCode == documentTypeCode &&
                        t.DocumentTemplateLocation != null &&
                        t.DocumentTemplateLocation.ToUpper() == fileName.ToUpper());

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.DocumentTemplateID != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Check if any contact documents have been created from this template
    /// </summary>
    /// <param name="documentTemplateId">Template ID to check</param>
    /// <returns>True if documents exist for this template</returns>
    public async Task<bool> DoDocumentsExistForTemplateAsync(int documentTemplateId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Check if any contact entities reference this template
        // The template could be used by CountyContact, OperatorContact, or BuyerContact
        var countyExists = await context.CountyContacts
            .AnyAsync(c => c.DocumentTemplateID == documentTemplateId);

        if (countyExists) return true;

        var operatorExists = await context.OperatorContacts
            .AnyAsync(c => c.DocumentTemplateID == documentTemplateId);

        if (operatorExists) return true;

        var buyerExists = await context.BuyerContacts
            .AnyAsync(c => c.DocumentTemplateID == documentTemplateId);

        return buyerExists;
    }


    /// <summary>
    /// Get document file URL for viewing/downloading
    /// </summary>
    public Task<string> GetDocumentUrlAsync(int documentTemplateId)
    {
        return Task.FromResult($"/api/documents/template/{documentTemplateId}");
    }

    /// <summary>
    /// Export document templates to Excel
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(string? documentTypeCode = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var repository = new DocumentTemplateRepository(context);
        var query = await repository.GetDocumentTemplatesAsync();

        if (!string.IsNullOrEmpty(documentTypeCode))
        {
            query = query.Where(d => d.DocumentTypeCode == documentTypeCode);
        }

        var templates = await query.ToListAsync();

        return await GenerateExcelBytes(templates);
    }

    /// <summary>
    /// Search documents using full-text search
    /// </summary>
    /// <param name="searchText">Search text (minimum 2 characters)</param>
    /// <param name="searchAcquisitions">True to search acquisition documents, false for check statements</param>
    /// <param name="includeAcquisitionDetails">True to include acquisition details in results</param>
    /// <returns>List of matching documents</returns>
    public Task<List<Models.DocumentSearchResult>> SearchDocumentsAsync(
        string searchText,
        bool searchAcquisitions = true,
        bool includeAcquisitionDetails = false)
    {
        // TODO: Implement actual DocuShare search integration
        // This is a stub that will need to be connected to the document storage system
        // 
        // The legacy implementation used SSRDocuShare.DocumentSearch class which:
        // 1. Connected to DocuShare server using credentials
        // 2. Searched within specified collection roots (Acquisition or Check Statement)
        // 3. Returned DocumentSearchItem objects with file info and optional acquisition ID
        // 4. Optionally loaded full acquisition details for each result
        //
        // Future implementation options:
        // - Azure Blob Storage with Azure Cognitive Search
        // - AWS S3 with OpenSearch
        // - Local file system with Lucene.NET
        // - Direct DocuShare API integration (if still in use)

        throw new NotImplementedException(
            "Document search is not yet implemented. " +
            "This feature requires integration with the document storage system.");
    }

    #region Private Helper Methods

    private IQueryable<DocumentTemplate> ApplyColumnFilters(IQueryable<DocumentTemplate> query, List<FilterDefinition>? filters)
    {
        if (filters == null || !filters.Any())
            return query;

        foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
        {
            query = ApplyFilter(query, filter);
        }

        return query;
    }

    private IQueryable<DocumentTemplate> ApplyFilter(IQueryable<DocumentTemplate> query, FilterDefinition filter)
    {
        var value = filter.Value?.ToLower() ?? string.Empty;

        return filter.Field switch
        {
            "DocumentTemplateID" when int.TryParse(filter.Value, out var id) =>
                query.Where(d => d.DocumentTemplateID == id),

            "DocumentTypeCode" =>
                query.Where(d => d.DocumentTypeCode != null && d.DocumentTypeCode.ToLower().Contains(value)),

            "DocumentTemplateDesc" =>
                query.Where(d => d.DocumentTemplateDesc != null && d.DocumentTemplateDesc.ToLower().Contains(value)),

            "DocumentTemplateLocation" =>
                query.Where(d => d.DocumentTemplateLocation != null && d.DocumentTemplateLocation.ToLower().Contains(value)),

            "DSFileID" =>
                query.Where(d => d.DSFileID != null && d.DSFileID.ToLower().Contains(value)),

            _ => query
        };
    }

    private IQueryable<DocumentTemplate> ApplySorting(IQueryable<DocumentTemplate> query, List<SortDefinition>? sortDefinitions)
    {
        if (sortDefinitions == null || !sortDefinitions.Any())
        {
            // Default sort by DocumentTypeCode, then DocumentTemplateDesc
            return query
                .OrderBy(d => d.DocumentTypeCode)
                .ThenBy(d => d.DocumentTemplateDesc);
        }

        IOrderedQueryable<DocumentTemplate>? orderedQuery = null;

        foreach (var sort in sortDefinitions)
        {
            orderedQuery = ApplySort(orderedQuery ?? query.OrderBy(d => 0), sort, orderedQuery == null);
        }

        return orderedQuery ?? query;
    }

    private IOrderedQueryable<DocumentTemplate> ApplySort(IOrderedQueryable<DocumentTemplate> query, SortDefinition sort, bool isFirst)
    {
        return sort.SortBy switch
        {
            "DocumentTemplateID" => isFirst
                ? (sort.Descending ? query.OrderByDescending(d => d.DocumentTemplateID) : query.OrderBy(d => d.DocumentTemplateID))
                : (sort.Descending ? query.ThenByDescending(d => d.DocumentTemplateID) : query.ThenBy(d => d.DocumentTemplateID)),

            "DocumentTypeCode" => isFirst
                ? (sort.Descending ? query.OrderByDescending(d => d.DocumentTypeCode) : query.OrderBy(d => d.DocumentTypeCode))
                : (sort.Descending ? query.ThenByDescending(d => d.DocumentTypeCode) : query.ThenBy(d => d.DocumentTypeCode)),

            "DocumentTemplateDesc" => isFirst
                ? (sort.Descending ? query.OrderByDescending(d => d.DocumentTemplateDesc) : query.OrderBy(d => d.DocumentTemplateDesc))
                : (sort.Descending ? query.ThenByDescending(d => d.DocumentTemplateDesc) : query.ThenBy(d => d.DocumentTemplateDesc)),

            "DocumentTemplateLocation" => isFirst
                ? (sort.Descending ? query.OrderByDescending(d => d.DocumentTemplateLocation) : query.OrderBy(d => d.DocumentTemplateLocation))
                : (sort.Descending ? query.ThenByDescending(d => d.DocumentTemplateLocation) : query.ThenBy(d => d.DocumentTemplateLocation)),

            _ => query
        };
    }

    private void InvalidateCache(int? documentTemplateId = null)
    {
        if (documentTemplateId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}_{documentTemplateId}");
        }

        _cache.Remove(AllDocumentsCacheKey);
        _cache.Remove(DocumentTypesCacheKey);
        _cachedDataService.InvalidateCache();

        _logger.LogInformation("Cache invalidated for DocumentTemplates");
    }

    private async Task<byte[]> GenerateExcelBytes(List<DocumentTemplate> templates)
    {
        // Placeholder - implement with ClosedXML or EPPlus
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Document type item for dropdown
/// </summary>
public class DocumentTypeItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

#endregion
