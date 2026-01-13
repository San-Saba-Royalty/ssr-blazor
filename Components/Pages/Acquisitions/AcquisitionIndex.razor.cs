using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Services;
using AcquisitionEntity = SSRBusiness.Entities.Acquisition;

namespace SSRBlazor.Components.Pages.Acquisitions;

public partial class AcquisitionIndex : ComponentBase
{
    // Grid reference
    private MudDataGrid<AcquisitionEntity>? _dataGrid;

    // State
    private AcquisitionEntity? _selectedItem;
    private string _displayMessage = string.Empty;
    private List<string> _validationErrors = new();
    private string _activeFilter = "All Records";
    private string _activeView = "All Fields";
    private int _totalCount = 0;
    private bool _showBarcodeDialog = false;

    // Available filters and views (loaded from service)
    private List<NamedFilter> _availableFilters = new();
    private List<NamedView> _availableViews = new();

    // Current view's visible columns
    private HashSet<string> _visibleColumns = new();

    protected override async Task OnInitializedAsync()
    {
        // Load available filters and views from service
        _availableFilters = await AcquisitionService.GetAvailableFiltersAsync();
        _availableViews = await AcquisitionService.GetAvailableViewsAsync();

        // Initialize with default view
        InitializeDefaultView();
    }

    private void InitializeDefaultView()
    {
        // Default visible columns matching "Summary View" plus a few more useful fields
        _visibleColumns = new HashSet<string>
        {
            "AcquisitionID",
            "AcquisitionNumber",
            "Buyer",
            "Assignee",
            "EffectiveDate",
            "DueDate",
            "PaidDate",
            "TotalBonus",
            "ClosingStatus",
            "SsrInPay"
        };
    }

    private bool IsColumnVisible(string columnName)
    {
        // If "All Fields" view, show all columns
        if (_activeView == "All Fields")
            return true;

        return _visibleColumns.Contains(columnName);
    }

    /// <summary>
    /// Server-side data loading for the grid using AcquisitionService
    /// </summary>
    private async Task<GridData<AcquisitionEntity>> LoadServerData(GridState<AcquisitionEntity> state)
    {
        try
        {
            // Convert MudBlazor sort definitions to our service format
            var sortDefinitions = state.SortDefinitions
                .Select(s => new SortDefinition
                {
                    SortBy = s.SortBy,
                    Descending = s.Descending
                })
                .ToList();

            // Convert MudBlazor filter definitions to our service format
            var filterDefinitions = state.FilterDefinitions
                .Where(f => f.Value != null)
                .Select(f => new FilterDefinition
                {
                    Field = f.Column?.PropertyName ?? string.Empty,
                    Operator = f.Operator?.ToString() ?? "Contains",
                    Value = f.Value?.ToString()
                })
                .ToList();

            // Call the service to get paginated data
            var result = await AcquisitionService.GetAcquisitionsPagedAsync(
                page: state.Page,
                pageSize: state.PageSize,
                activeFilter: _activeFilter,
                sortDefinitions: sortDefinitions,
                filterDefinitions: filterDefinitions
            );

            _totalCount = result.TotalCount;

            return new GridData<AcquisitionEntity>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            return new GridData<AcquisitionEntity>
            {
                Items = Enumerable.Empty<AcquisitionEntity>(),
                TotalItems = 0
            };
        }
    }

    /// <summary>
    /// Handle row click to select (double-click to edit)
    /// </summary>
    private void OnRowClick(DataGridRowClickEventArgs<AcquisitionEntity> args)
    {
        _selectedItem = args.Item;

        // Double-click navigates to edit
        if (args.MouseEventArgs.Detail == 2)
        {
            EditSelectedAcquisition();
        }
    }

    /// <summary>
    /// Row styling function - highlight overdue or special status
    /// </summary>
    private string RowStyleFunc(AcquisitionEntity item, int index)
    {
        // Highlight items with liens in light orange
        if (item.Liens)
            return "background-color: #fff3e0;";

        // Highlight overdue items in light red
        if (item.DueDate.HasValue && item.DueDate.Value < DateTime.Today && item.PaidDate == null)
            return "background-color: #ffebee;";

        // Highlight items in pay in light green
        if (item.SsrInPay == "Yes")
            return "background-color: #e8f5e9;";

        // Highlight selected row
        if (_selectedItem?.AcquisitionID == item.AcquisitionID)
            return "background-color: #e3f2fd;";

        return string.Empty;
    }

    /// <summary>
    /// Navigate to Letter Agreements page
    /// </summary>
    private void GotoLetterAgreements()
    {
        Navigation.NavigateTo("/letteragreements");
    }

    /// <summary>
    /// Apply a named filter from the dropdown
    /// </summary>
    private async Task ApplyFilter(string filterName)
    {
        _activeFilter = filterName;
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
        Snackbar.Add($"Filter applied: {filterName}", Severity.Info);
    }

    /// <summary>
    /// Apply a named view (controls column visibility)
    /// </summary>
    private void ApplyView(string viewName)
    {
        _activeView = viewName;

        // Find the view definition from service
        var view = _availableViews.FirstOrDefault(v => v.Name == viewName);

        if (view?.VisibleColumns != null)
        {
            _visibleColumns = new HashSet<string>(view.VisibleColumns);
        }
        else
        {
            // "All Fields" - clear the set so IsColumnVisible returns true for all
            _visibleColumns.Clear();
        }

        StateHasChanged();
        Snackbar.Add($"View applied: {viewName}", Severity.Info);
    }

    /// <summary>
    /// Reset all filters to default
    /// </summary>
    private async Task ResetFilters()
    {
        _activeFilter = "All Records";
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
        Snackbar.Add("Filters reset", Severity.Info);
    }

    /// <summary>
    /// Reset sort order to default (AcquisitionID desc)
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
    /// Navigate to new acquisition form
    /// </summary>
    private void AddNewAcquisition()
    {
        Navigation.NavigateTo("/acquisition/new");
    }

    /// <summary>
    /// Navigate to edit form for selected acquisition
    /// </summary>
    private void EditSelectedAcquisition()
    {
        if (_selectedItem != null)
        {
            Navigation.NavigateTo($"/acquisition/edit/{_selectedItem.AcquisitionID}");
        }
        else
        {
            Snackbar.Add("Please select an acquisition to edit", Severity.Warning);
        }
    }

    /// <summary>
    /// Copy selected acquisition using service
    /// </summary>
    /// <summary>
    /// Navigate to copy form for selected acquisition
    /// </summary>
    private void CopySelectedAcquisition()
    {
        if (_selectedItem != null)
        {
            Navigation.NavigateTo($"/acquisition/copy/{_selectedItem.AcquisitionID}");
        }
        else
        {
            Snackbar.Add("Please select an acquisition to copy", Severity.Warning);
        }
    }

    /// <summary>
    /// Delete selected acquisition using service
    /// </summary>
    private async Task DeleteSelectedAcquisition()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select an acquisition to delete", Severity.Warning);
            return;
        }

        var confirm = await DialogService.ShowMessageBox(
            "Delete Acquisition",
            $"Are you sure you want to delete Acquisition #{_selectedItem.AcquisitionID}? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel",
            options: new DialogOptions { CloseButton = true });

        if (confirm == true)
        {
            try
            {
                await AcquisitionService.DeleteAsync(_selectedItem.AcquisitionID);
                Snackbar.Add($"Acquisition #{_selectedItem.AcquisitionID} deleted", Severity.Success);
                _selectedItem = null;

                if (_dataGrid != null)
                {
                    await _dataGrid.ReloadServerData();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error deleting acquisition: {ex.Message}", Severity.Error);
            }
        }
    }

    /// <summary>
    /// Show barcode cover sheet generation dialog
    /// </summary>
    private void GenerateBarcodeCoverSheet()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select an acquisition", Severity.Warning);
            return;
        }

        _showBarcodeDialog = true;
    }

    /// <summary>
    /// Generate and open barcode cover sheet
    /// </summary>
    private async Task ConfirmGenerateBarcode()
    {
        if (_selectedItem == null) return;

        try
        {
            _showBarcodeDialog = false;

            // Get the URL for the barcode document from service
            var fileUrl = await AcquisitionService.GenerateBarcodeCoverSheetAsync(_selectedItem.AcquisitionID);

            // Open in new window
            await JSRuntime.InvokeVoidAsync("open", fileUrl, "_blank");

            Snackbar.Add("Cover sheet generated", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating cover sheet: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Export current grid data to Excel
    /// </summary>
    private async Task ExportToExcel()
    {
        try
        {
            Snackbar.Add("Preparing export...", Severity.Info);

            var fileBytes = await AcquisitionService.ExportToExcelAsync(_activeFilter);

            if (fileBytes.Length == 0)
            {
                Snackbar.Add("No data to export or export not implemented", Severity.Warning);
                return;
            }

            // Trigger download via JS interop
            var fileName = $"MineralAcquisitions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
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