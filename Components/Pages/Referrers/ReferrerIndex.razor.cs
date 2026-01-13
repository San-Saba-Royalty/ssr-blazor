using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Services;

namespace SSRBlazor.Components.Pages.Referrers;

public partial class ReferrerIndex : ComponentBase
{
    // Grid reference
    private MudDataGrid<Referrer>? _dataGrid;
    private MudMenu? _contextMenu;

    // State
    private Referrer? _selectedItem;
    private string _displayMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private int _totalCount = 0;
    private bool _showDeleteDialog = false;
    private bool _hasAssociatedAcquisitions = false;

    /// <summary>
    /// Server-side data loading for the grid
    /// </summary>
    private async Task<GridData<Referrer>> LoadServerData(GridState<Referrer> state)
    {
        try
        {
            // Convert MudBlazor sort definitions to service format
            var sortDefinitions = state.SortDefinitions
                .Select(s => new SortDefinition
                {
                    SortBy = s.SortBy,
                    Descending = s.Descending
                })
                .ToList();

            // Convert MudBlazor filter definitions to service format
            var filterDefinitions = state.FilterDefinitions
                .Where(f => f.Value != null)
                .Select(f => new FilterDefinition
                {
                    Field = f.Column?.PropertyName ?? string.Empty,
                    Operator = f.Operator?.ToString() ?? "Contains",
                    Value = f.Value?.ToString()
                })
                .ToList();

            // Call the service
            var result = await ReferrerService.GetReferrersPagedAsync(
                page: state.Page,
                pageSize: state.PageSize,
                sortDefinitions: sortDefinitions,
                filterDefinitions: filterDefinitions
            );

            _totalCount = result.TotalCount;

            return new GridData<Referrer>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            return new GridData<Referrer>
            {
                Items = Enumerable.Empty<Referrer>(),
                TotalItems = 0
            };
        }
    }

    /// <summary>
    /// Handle row click for selection, double-click to edit
    /// </summary>
    private void OnRowClick(DataGridRowClickEventArgs<Referrer> args)
    {
        _selectedItem = args.Item;

        // Double-click navigates to edit (matching OnRowDblClick behavior)
        if (args.MouseEventArgs.Detail == 2)
        {
            EditReferrer(args.Item);
        }
    }

    /// <summary>
    /// Row styling function
    /// </summary>
    private string RowStyleFunc(Referrer item, int index)
    {
        // Highlight selected row
        if (_selectedItem?.ReferrerID == item.ReferrerID)
            return "background-color: #e3f2fd;";

        return string.Empty;
    }

    /// <summary>
    /// Get full address for display
    /// </summary>
    private string GetFullAddress(Referrer referrer)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(referrer.AddressLine1))
            parts.Add(referrer.AddressLine1);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Navigate to add new referrer
    /// </summary>
    private void AddNewReferrer()
    {
        Navigation.NavigateTo("/referrer/edit");
    }

    /// <summary>
    /// Navigate to edit referrer
    /// </summary>
    private void EditReferrer(Referrer referrer)
    {
        Navigation.NavigateTo($"/referrer/edit/{referrer.ReferrerID}");
    }

    /// <summary>
    /// Edit selected referrer
    /// </summary>
    private void EditSelectedReferrer()
    {
        if (_selectedItem != null)
        {
            EditReferrer(_selectedItem);
        }
        else
        {
            Snackbar.Add("Please select a referrer to edit", Severity.Warning);
        }
    }

    /// <summary>
    /// Show delete confirmation dialog
    /// </summary>
    private async Task DeleteSelectedReferrer()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select a referrer to delete", Severity.Warning);
            return;
        }

        // Check for associated acquisitions
        _hasAssociatedAcquisitions = await ReferrerService.HasAssociatedAcquisitionsAsync(_selectedItem.ReferrerID);

        _showDeleteDialog = true;
    }

    /// <summary>
    /// Confirm and execute delete
    /// </summary>
    private async Task ConfirmDelete()
    {
        if (_selectedItem == null) return;

        try
        {
            await ReferrerService.DeleteAsync(_selectedItem.ReferrerID);

            Snackbar.Add("Referrer deleted successfully", Severity.Success);

            _showDeleteDialog = false;
            _selectedItem = null;
            _hasAssociatedAcquisitions = false;

            if (_dataGrid != null)
            {
                await _dataGrid.ReloadServerData();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting referrer: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Reset all filters
    /// </summary>
    private async Task ResetFilters()
    {
        _selectedItem = null;

        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }

        Snackbar.Add("Filters reset", Severity.Info);
    }

    /// <summary>
    /// Reset sort order
    /// </summary>
    private async Task ResetSort()
    {
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }

        Snackbar.Add("Sort order reset", Severity.Info);
    }

    /// <summary>
    /// Export to Excel
    /// </summary>
    private async Task ExportToExcel()
    {
        try
        {
            Snackbar.Add("Preparing export...", Severity.Info);

            var fileBytes = await ReferrerService.ExportToExcelAsync();

            if (fileBytes.Length == 0)
            {
                Snackbar.Add("No data to export or export not implemented", Severity.Warning);
                return;
            }

            var fileName = $"Referrers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            await JSRuntime.InvokeVoidAsync(
                "downloadFile",
                fileName,
                Convert.ToBase64String(fileBytes),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            Snackbar.Add("Export complete", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error exporting data: {ex.Message}", Severity.Error);
        }
    }
}