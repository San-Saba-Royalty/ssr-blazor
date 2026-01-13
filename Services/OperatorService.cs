using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;
using ClosedXML.Excel;

namespace SSRBlazor.Services;

/// <summary>
/// Service for Operator business logic
/// </summary>
public class OperatorService
{
    private readonly OperatorRepository _repository;
    private readonly ILogger<OperatorService> _logger;

    public OperatorService(
        OperatorRepository repository,
        ILogger<OperatorService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    #region Read Operations

    /// <summary>
    /// Get operators with server-side paging, filtering, and sorting
    /// </summary>
    public async Task<GridData<Operator>> GetOperatorsPagedAsync(
        GridState<Operator> state,
        Dictionary<string, string>? columnFilters = null)
    {
        var query = _repository.GetOperatorsAsync();

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
            query = query.OrderBy(o => o.OperatorName);
        }

        // Apply paging
        var items = await query
            .Skip(state.Page * state.PageSize)
            .Take(state.PageSize)
            .ToListAsync();

        return new GridData<Operator>
        {
            Items = items,
            TotalItems = totalItems
        };
    }

    /// <summary>
    /// Get operator by ID
    /// </summary>
    public async Task<Operator?> GetByIdAsync(int operatorId)
    {
        return await _repository.GetByIdAsync(operatorId);
    }

    /// <summary>
    /// Get all operators
    /// </summary>
    public async Task<List<Operator>> GetAllAsync()
    {
        var operators = await _repository.GetOperatorsAsync()
            .OrderBy(o => o.OperatorName)
            .ToListAsync();
        return operators;
    }

    #endregion

    #region Write Operations

    /// <summary>
    /// Create new operator
    /// </summary>
    public async Task<(bool Success, Operator? Operator, string? Error)> CreateAsync(Operator operatorEntity)
    {
        try
        {
            // Check for duplicate name
            if (await _repository.OperatorNameExistsAsync(operatorEntity.OperatorName!))
            {
                return (false, null, "An operator with this name already exists");
            }

            var created = await _repository.AddAsync(operatorEntity);
            return (true, created, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating operator");
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Update existing operator
    /// </summary>
    public async Task<(bool Success, Operator? Operator, string? Error)> UpdateAsync(Operator operatorEntity)
    {
        try
        {
            // Check for duplicate name
            if (await _repository.OperatorNameExistsAsync(operatorEntity.OperatorName!, operatorEntity.OperatorID))
            {
                return (false, null, "An operator with this name already exists");
            }

            var updated = await _repository.UpdateAsync(operatorEntity);
            return (true, updated, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operator {OperatorId}", operatorEntity.OperatorID);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete operator
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteAsync(int operatorId)
    {
        try
        {
            var operatorEntity = await _repository.GetByIdAsync(operatorId);
            if (operatorEntity == null)
            {
                return (false, "Operator not found");
            }

            await _repository.DeleteAsync(operatorEntity);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting operator {OperatorId}", operatorId);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Check if operator has associated acquisitions
    /// </summary>
    public async Task<bool> HasAssociatedAcquisitionsAsync(int operatorId)
    {
        return await _repository.HasAssociatedAcquisitionsAsync(operatorId);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Check if operator name exists (excluding specific ID)
    /// </summary>
    public async Task<bool> DoesOperatorNameExistAsync(string name, int? excludeId = null)
    {
        return await _repository.OperatorNameExistsAsync(name, excludeId);
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    public Task<bool> ValidateEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return Task.FromResult(true);

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return Task.FromResult(addr.Address == email);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Operator Contact Operations

    /// <summary>
    /// Get operator with all contacts
    /// </summary>
    public async Task<Operator?> GetOperatorWithContactsAsync(int operatorId)
    {
        return await _repository.GetWithContactsAsync(operatorId);
    }

    /// <summary>
    /// Get all contacts for an operator
    /// </summary>
    public async Task<List<OperatorContact>> GetContactsByOperatorIdAsync(int operatorId)
    {
        return await _repository.GetContactsByOperatorIdAsync(operatorId);
    }

    /// <summary>
    /// Get contact by ID
    /// </summary>
    public async Task<OperatorContact?> GetContactByIdAsync(int contactId)
    {
        return await _repository.GetContactByIdAsync(contactId);
    }

    /// <summary>
    /// Create new operator contact
    /// </summary>
    public async Task<(bool Success, OperatorContact? Contact, string? Error)> CreateContactAsync(OperatorContact contact)
    {
        try
        {
            var created = await _repository.AddContactAsync(contact);
            return (true, created, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact for operator {OperatorId}", contact.OperatorID);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Update existing operator contact
    /// </summary>
    public async Task<(bool Success, OperatorContact? Contact, string? Error)> UpdateContactAsync(OperatorContact contact)
    {
        try
        {
            var updated = await _repository.UpdateContactAsync(contact);
            return (true, updated, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}", contact.OperatorContactID);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete operator contact
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteContactAsync(int contactId)
    {
        try
        {
            var contact = await _repository.GetContactByIdAsync(contactId);
            if (contact == null) return (false, "Contact not found");

            await _repository.DeleteContactAsync(contact);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactId}", contactId);
            return (false, ex.Message);
        }
    }

    #endregion

    #region Check Statement
    private string GetCheckStatementPageUrl(int operatorId)
    {
        return $"/operator/check-statement-capture?oid={operatorId}";
    }

    /// <summary>
    /// Generate bulk check statement cover sheets
    /// </summary>
    public async Task<(bool Success, string? FilePath, string? Error)> GenerateCheckStatementsAsync(List<CheckStatementRequest> requests)
    {
        try
        {
            if (requests == null || !requests.Any())
                return (false, null, "No requests provided");

            _logger.LogInformation("Bulk check statement generation requested for {Count} operators", requests.Count);

            // Placeholder - in real implementation, this would call a document generation engine
            // and return a merged document. For now, we simulate success.
            await Task.Delay(1000);

            return (true, $"/api/documents/checkstatement/bulk?t={DateTime.Now.Ticks}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bulk check statements");
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Generate check statement cover sheet for operator
    /// </summary>
    public async Task<(bool Success, string? FilePath, string? Error)> GenerateCheckStatementAsync(int operatorId, DateTime? checkDate = null, int copies = 1)
    {
        return await GenerateCheckStatementsAsync(new List<CheckStatementRequest>
        {
            new CheckStatementRequest
            {
                OperatorID = operatorId,
                CheckDate = checkDate,
                NumberOfCopies = copies
            }
        });
    }

    #endregion

    #region Export

    /// <summary>
    /// Export operators to Excel
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(Dictionary<string, string>? columnFilters = null)
    {
        var query = _repository.GetOperatorsAsync();

        if (columnFilters != null && columnFilters.Any())
        {
            query = ApplyColumnFilters(query, columnFilters);
        }

        var operators = await query
            .OrderBy(o => o.OperatorName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Operators");

        // Headers
        var headers = new[] { "Operator Name", "Contact Name", "Contact Email", "Contact Phone",
            "Contact Fax", "Address", "City", "State", "Zip Code" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        for (int row = 0; row < operators.Count; row++)
        {
            var op = operators[row];
            var address = GetFullAddress(op);

            worksheet.Cell(row + 2, 1).Value = op.OperatorName;
            worksheet.Cell(row + 2, 2).Value = op.ContactName;
            worksheet.Cell(row + 2, 3).Value = op.ContactEmail;
            worksheet.Cell(row + 2, 4).Value = op.ContactPhone;
            worksheet.Cell(row + 2, 5).Value = op.ContactFax;
            worksheet.Cell(row + 2, 6).Value = address;
            worksheet.Cell(row + 2, 7).Value = op.City;
            worksheet.Cell(row + 2, 8).Value = op.StateCode;
            worksheet.Cell(row + 2, 9).Value = op.ZipCode;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Private Methods

    private static IQueryable<Operator> ApplyColumnFilters(
        IQueryable<Operator> query,
        Dictionary<string, string> filters)
    {
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Value))
                continue;

            var value = filter.Value.ToLower();

            query = filter.Key switch
            {
                "OperatorName" => query.Where(o => o.OperatorName != null && o.OperatorName.ToLower().Contains(value)),
                "ContactName" => query.Where(o => o.ContactName != null && o.ContactName.ToLower().Contains(value)),
                "ContactEmail" => query.Where(o => o.ContactEmail != null && o.ContactEmail.ToLower().Contains(value)),
                "ContactPhone" => query.Where(o => o.ContactPhone != null && o.ContactPhone.Contains(value)),
                "ContactFax" => query.Where(o => o.ContactFax != null && o.ContactFax.Contains(value)),
                "AddressLine1" => query.Where(o => o.AddressLine1 != null && o.AddressLine1.ToLower().Contains(value)),
                "City" => query.Where(o => o.City != null && o.City.ToLower().Contains(value)),
                "StateCode" => query.Where(o => o.StateCode != null && o.StateCode.ToLower().Contains(value)),
                "ZipCode" => query.Where(o => o.ZipCode != null && o.ZipCode.Contains(value)),
                _ => query
            };
        }

        return query;
    }

    private static IQueryable<Operator> ApplyFilter(
        IQueryable<Operator> query,
        IFilterDefinition<Operator> filter)
    {
        if (filter.Column?.PropertyName == null || filter.Operator == null)
            return query;

        var propertyName = filter.Column.PropertyName;
        var filterValue = filter.Value?.ToString()?.ToLower() ?? string.Empty;

        if (string.IsNullOrEmpty(filterValue))
            return query;

        return propertyName switch
        {
            "OperatorName" => query.Where(o => o.OperatorName != null && o.OperatorName.ToLower().Contains(filterValue)),
            "ContactName" => query.Where(o => o.ContactName != null && o.ContactName.ToLower().Contains(filterValue)),
            "ContactEmail" => query.Where(o => o.ContactEmail != null && o.ContactEmail.ToLower().Contains(filterValue)),
            "ContactPhone" => query.Where(o => o.ContactPhone != null && o.ContactPhone.Contains(filterValue)),
            "ContactFax" => query.Where(o => o.ContactFax != null && o.ContactFax.Contains(filterValue)),
            "City" => query.Where(o => o.City != null && o.City.ToLower().Contains(filterValue)),
            "StateCode" => query.Where(o => o.StateCode != null && o.StateCode.ToLower().Contains(filterValue)),
            "ZipCode" => query.Where(o => o.ZipCode != null && o.ZipCode.Contains(filterValue)),
            _ => query
        };
    }

    private static IQueryable<Operator> ApplySorting(
        IQueryable<Operator> query,
        ICollection<SortDefinition<Operator>> sortDefinitions)
    {
        IOrderedQueryable<Operator>? orderedQuery = null;

        foreach (var sort in sortDefinitions)
        {
            if (sort.SortBy == null) continue;

            var propertyName = sort.SortBy;
            var descending = sort.Descending;

            if (orderedQuery == null)
            {
                orderedQuery = propertyName switch
                {
                    "OperatorName" => descending
                        ? query.OrderByDescending(o => o.OperatorName)
                        : query.OrderBy(o => o.OperatorName),
                    "ContactName" => descending
                        ? query.OrderByDescending(o => o.ContactName)
                        : query.OrderBy(o => o.ContactName),
                    "ContactEmail" => descending
                        ? query.OrderByDescending(o => o.ContactEmail)
                        : query.OrderBy(o => o.ContactEmail),
                    "ContactPhone" => descending
                        ? query.OrderByDescending(o => o.ContactPhone)
                        : query.OrderBy(o => o.ContactPhone),
                    "City" => descending
                        ? query.OrderByDescending(o => o.City)
                        : query.OrderBy(o => o.City),
                    "StateCode" => descending
                        ? query.OrderByDescending(o => o.StateCode)
                        : query.OrderBy(o => o.StateCode),
                    "ZipCode" => descending
                        ? query.OrderByDescending(o => o.ZipCode)
                        : query.OrderBy(o => o.ZipCode),
                    _ => query.OrderBy(o => o.OperatorName)
                };
            }
            else
            {
                orderedQuery = propertyName switch
                {
                    "OperatorName" => descending
                        ? orderedQuery.ThenByDescending(o => o.OperatorName)
                        : orderedQuery.ThenBy(o => o.OperatorName),
                    "ContactName" => descending
                        ? orderedQuery.ThenByDescending(o => o.ContactName)
                        : orderedQuery.ThenBy(o => o.ContactName),
                    "ContactEmail" => descending
                        ? orderedQuery.ThenByDescending(o => o.ContactEmail)
                        : orderedQuery.ThenBy(o => o.ContactEmail),
                    "ContactPhone" => descending
                        ? orderedQuery.ThenByDescending(o => o.ContactPhone)
                        : orderedQuery.ThenBy(o => o.ContactPhone),
                    "City" => descending
                        ? orderedQuery.ThenByDescending(o => o.City)
                        : orderedQuery.ThenBy(o => o.City),
                    "StateCode" => descending
                        ? orderedQuery.ThenByDescending(o => o.StateCode)
                        : orderedQuery.ThenBy(o => o.StateCode),
                    "ZipCode" => descending
                        ? orderedQuery.ThenByDescending(o => o.ZipCode)
                        : orderedQuery.ThenBy(o => o.ZipCode),
                    _ => orderedQuery
                };
            }
        }

        return orderedQuery ?? query.OrderBy(o => o.OperatorName);
    }

    private static string GetFullAddress(Operator op)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(op.AddressLine1))
            parts.Add(op.AddressLine1);
        if (!string.IsNullOrWhiteSpace(op.AddressLine2))
            parts.Add(op.AddressLine2);

        return string.Join(", ", parts);
    }

    #endregion
}

/// <summary>
/// Request for check statement generation
/// </summary>
public class CheckStatementRequest
{
    public int OperatorID { get; set; }
    public string? OperatorName { get; set; }
    public DateTime? CheckDate { get; set; }
    public int NumberOfCopies { get; set; } = 1;
}