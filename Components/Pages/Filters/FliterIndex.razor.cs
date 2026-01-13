using Microsoft.AspNetCore.Components;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Services;

namespace SSRBlazor.Components.Pages.Filters;

public partial class FilterIndex : ComponentBase
{
    // State
    private List<Filter> _filters = new();
    private List<FilterFieldDefinition> _availableFields = new();
    private List<FilterDetailModel> _filterDetails = new();

    private int? _selectedFilterId;
    private string _filterName = string.Empty;
    private bool _isNewFilter = false;

    private string _displayMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _showDeleteDialog = false;

    // Computed property
    private bool CanSave => !string.IsNullOrWhiteSpace(_filterName) &&
                            _filterDetails.Any(d => !string.IsNullOrEmpty(d.FieldName));

    protected override async Task OnInitializedAsync()
    {
        await LoadFiltersAsync();
        _availableFields = FilterService.GetAvailableFields();
        InitializeEmptyFilterDetails();
    }

    /// <summary>
    /// Load all filters for dropdown
    /// </summary>
    private async Task LoadFiltersAsync()
    {
        try
        {
            _filters = await FilterService.GetAllFiltersAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading filters: {ex.Message}";
        }
    }

    /// <summary>
    /// Initialize empty filter detail rows
    /// </summary>
    private void InitializeEmptyFilterDetails()
    {
        _filterDetails = new List<FilterDetailModel>();
        for (int i = 1; i <= 5; i++)
        {
            _filterDetails.Add(new FilterDetailModel { RowNumber = i });
        }
    }

    /// <summary>
    /// Handle filter selection from dropdown
    /// </summary>
    private async Task OnFilterSelected(int? filterId)
    {
        _selectedFilterId = filterId;
        _isNewFilter = false;

        if (filterId.HasValue)
        {
            await LoadFilterDetailsAsync(filterId.Value);
        }
        else
        {
            _filterName = string.Empty;
            InitializeEmptyFilterDetails();
        }
    }

    /// <summary>
    /// Load filter details when a filter is selected
    /// </summary>
    private async Task LoadFilterDetailsAsync(int filterId)
    {
        try
        {
            var filter = await FilterService.GetFilterByIdAsync(filterId);
            if (filter != null)
            {
                _filterName = filter.FilterName ?? string.Empty;

                // Get filter fields from service (which handles mapping from database)
                _filterDetails = await FilterService.GetFilterFieldsAsync(filterId);

                // Ensure at least 5 rows
                while (_filterDetails.Count < 5)
                {
                    _filterDetails.Add(new FilterDetailModel { RowNumber = _filterDetails.Count + 1 });
                }
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading filter details: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle field selection change - update comparison types
    /// </summary>
    private void OnFieldChanged(FilterDetailModel detail, string fieldName)
    {
        detail.FieldName = fieldName;

        // Reset comparison and value when field changes
        if (string.IsNullOrEmpty(fieldName))
        {
            detail.ComparisonType = string.Empty;
            detail.CompareValue = string.Empty;
        }
        else
        {
            // Set default comparison type for the field
            var comparisonTypes = GetComparisonTypesForField(fieldName);
            if (comparisonTypes.Any())
            {
                detail.ComparisonType = comparisonTypes.First().Value;
            }
        }

        StateHasChanged();
    }

    /// <summary>
    /// Get comparison types for a specific field
    /// </summary>
    private List<ComparisonTypeOption> GetComparisonTypesForField(string? fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            return new List<ComparisonTypeOption>();

        return FilterService.GetComparisonTypes(fieldName);
    }

    /// <summary>
    /// Check if a value is required for the comparison type
    /// </summary>
    private bool IsValueRequired(string? comparisonType)
    {
        if (string.IsNullOrEmpty(comparisonType))
            return true;

        // These comparison types don't require a value
        var noValueRequired = new[] { "IsEmpty", "IsNotEmpty", "IsNull", "IsNotNull", "IsTrue", "IsFalse" };
        return !noValueRequired.Contains(comparisonType);
    }

    /// <summary>
    /// Start creating a new filter
    /// </summary>
    private void NewFilter()
    {
        _isNewFilter = true;
        _selectedFilterId = null;
        _filterName = string.Empty;
        InitializeEmptyFilterDetails();

        _displayMessage = "Enter a name for the new filter and define the criteria.";
    }

    /// <summary>
    /// Save the filter
    /// </summary>
    private async Task SaveFilter()
    {
        if (string.IsNullOrWhiteSpace(_filterName))
        {
            Snackbar.Add("Filter name is required", Severity.Warning);
            return;
        }

        var validDetails = _filterDetails
            .Where(d => !string.IsNullOrEmpty(d.FieldName) && !string.IsNullOrEmpty(d.ComparisonType))
            .ToList();

        if (!validDetails.Any())
        {
            Snackbar.Add("At least one filter criteria is required", Severity.Warning);
            return;
        }

        try
        {
            if (_isNewFilter)
            {
                var filter = new Filter { FilterName = _filterName };
                var newId = await FilterService.CreateFilterAsync(filter, validDetails);

                Snackbar.Add("Filter created successfully", Severity.Success);

                // Reload filters and select the new one
                await LoadFiltersAsync();
                _selectedFilterId = newId;
                _isNewFilter = false;
            }
            else if (_selectedFilterId.HasValue)
            {
                var filter = new Filter
                {
                    FilterID = _selectedFilterId.Value,
                    FilterName = _filterName
                };
                await FilterService.UpdateFilterAsync(filter, validDetails);

                Snackbar.Add("Filter updated successfully", Severity.Success);
                await LoadFiltersAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving filter: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Cancel current operation
    /// </summary>
    private void Cancel()
    {
        _isNewFilter = false;
        _selectedFilterId = null;
        _filterName = string.Empty;
        InitializeEmptyFilterDetails();
        _displayMessage = string.Empty;
        _errorMessage = string.Empty;
    }

    /// <summary>
    /// Show delete confirmation dialog
    /// </summary>
    private void DeleteFilter()
    {
        if (_selectedFilterId == null || _isNewFilter)
        {
            Snackbar.Add("Please select a filter to delete", Severity.Warning);
            return;
        }

        _showDeleteDialog = true;
    }

    /// <summary>
    /// Confirm and execute delete
    /// </summary>
    private async Task ConfirmDelete()
    {
        if (_selectedFilterId == null) return;

        try
        {
            await FilterService.DeleteFilterAsync(_selectedFilterId.Value);

            Snackbar.Add("Filter deleted successfully", Severity.Success);

            _showDeleteDialog = false;
            Cancel();
            await LoadFiltersAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting filter: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Apply filter and navigate to Acquisitions
    /// </summary>
    private void ApplyFilter()
    {
        if (_selectedFilterId == null && !_isNewFilter)
        {
            Snackbar.Add("Please select or save a filter first", Severity.Warning);
            return;
        }

        if (_selectedFilterId.HasValue)
        {
            var queryString = FilterService.BuildFilterQueryString(_selectedFilterId.Value);
            Navigation.NavigateTo($"/acquisitions{queryString}");
        }
        else
        {
            Snackbar.Add("Please save the filter before applying", Severity.Warning);
        }
    }

    /// <summary>
    /// Add a new criteria row
    /// </summary>
    private void AddRow()
    {
        if (_filterDetails.Count >= 10)
        {
            Snackbar.Add("Maximum 10 criteria rows allowed", Severity.Warning);
            return;
        }

        _filterDetails.Add(new FilterDetailModel { RowNumber = _filterDetails.Count + 1 });
    }

    /// <summary>
    /// Remove a criteria row
    /// </summary>
    private void RemoveRow(FilterDetailModel detail)
    {
        if (_filterDetails.Count <= 1)
        {
            Snackbar.Add("At least one criteria row is required", Severity.Warning);
            return;
        }

        _filterDetails.Remove(detail);

        // Renumber rows
        for (int i = 0; i < _filterDetails.Count; i++)
        {
            _filterDetails[i].RowNumber = i + 1;
        }
    }
}